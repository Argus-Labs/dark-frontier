package system

import (
	"fmt"
	comp "github.com/argus-labs/darkfrontier-backend/cardinal/component"
	"github.com/argus-labs/darkfrontier-backend/cardinal/tx"
	"github.com/argus-labs/darkfrontier-backend/cardinal/utils"
	"github.com/ericlagergren/decimal"
	"pkg.world.dev/world-engine/cardinal"
	"strconv"
)

var RebuildIndex = true

func SendEnergySystem(wCtx cardinal.WorldContext) error {
	log := wCtx.Logger()

	// 1. Check that the game timer is not over, if it is, exit
	err := checkTimer(wCtx)
	if err != nil {
		log.Debug().Msg(err.Error())
		return nil
	}

	// 1b. Check if indexes need to be rebuilt, if so, rebuild
	if RebuildIndex {
		err = rebuildDefaultsAndComponentIndexes(wCtx)
		if err != nil {
			log.Debug().Msg(err.Error())
			return nil
		} else {
			RebuildIndex = false
		}
	}

	// 2. For each ship send transactions,
	tx.SendEnergy.Each(wCtx, func(t cardinal.TxData[tx.SendEnergyMsg]) (result tx.SendEnergyReply, err error) {
		txData := t.Msg()
		txSig := t.Tx()

		log.Debug().Msgf("Send Energy payload: planetFrom: %s, planetTo: %s, energy: %d", txData.LocationHashFrom, txData.LocationHashTo, txData.Energy)

		// 1. PRE-CONDITION: Check that the LocationHash is well formatted
		if err = txData.Validate(); err != nil {
			log.Error().Err(err).Msg("")
			return result, err
		}

		// 2a. PRE-CONDITION: Verify that the origin planet exist
		planetFromEntity, ok := comp.LoadPlanetComponent(txData.LocationHashFrom)
		if ok == false {
			err = fmt.Errorf("no planet exists at the following location hash %s", txData.LocationHashFrom)
			log.Error().Err(err).Msg("")
			return result, err
		}
		planetFrom := planetFromEntity.Component

		// 2b. PRE-CONDITION: Verify that the origin planet is owned by the player
		if planetFrom.OwnerPersonaTag != txSig.PersonaTag {
			err = fmt.Errorf("player with persona %s does not own planet with location hash %s", txSig.PersonaTag, txData.LocationHashFrom)
			log.Error().Err(err).Msg("")
			return result, err
		}

		// Lazy energy refill
		log.Debug().Msgf("Applying lazy energy refill to planet: %s", planetFrom.LocationHash)
		normalizedRefillAge := utils.NormalizedRefillAge(planetFrom.LastUpdateRefillAge, planetFrom.LastUpdateTick, utils.IntToDec(int(wCtx.CurrentTick())), utils.ScaleUpByTickRate(planetFrom.EnergyRefill))
		planetFrom.EnergyCurrent = utils.EnergyLevel(planetFrom.EnergyMax, normalizedRefillAge)
		planetFrom.LastUpdateTick = utils.IntToDec(int(wCtx.CurrentTick()))
		planetFrom.LastUpdateRefillAge = normalizedRefillAge

		log.Debug().Msgf("Updated energy of planet with location hash %s to %s", planetFrom.LocationHash, utils.DecToStr(planetFrom.EnergyCurrent))

		// 2c. PRE-CONDITION: Verify that the origin planet has enough energy
		if utils.LessThan(planetFrom.EnergyCurrent, utils.Int64ToDec(txData.Energy)) {
			err = fmt.Errorf("origin planet with hash %s did not have enough energy", txData.LocationHashFrom)
			log.Error().Err(err).Msg("")
			return result, err
		}

		// 2d. PRE-CONDITION: Verify that the destination planet exist
		planetToStats, err := utils.GetPlanetStatsByLocationHash(txData.LocationHashTo, txData.PerlinTo)
		if err != nil {
			err = fmt.Errorf("no destination planet exists with the following hash: %s: %w", txData.LocationHashTo, err)
			log.Error().Err(err).Msg("")
			return result, err
		}

		// 2e. PRE-CONDITION: Verify ZK proof
		// Prove: I know (x1,y1,x2,y2,p2,r2,distMax) such that:
		// - x2^2 + y2^2 <= r^2
		// - perlin(x2, y2) = perlin2
		// - (x1-x2)^2 + (y1-y2)^2 <= distMax^2
		// - MiMCSponge(x1,y1) = pub1
		// - MiMCSponge(x2,y2) = pub2
		publicWitness, err := txData.CreatePublicWitness()
		if err != nil {
			err = fmt.Errorf("error creating public witness for send energy tx: %w", err)
			log.Error().Err(err).Msg("")
			return result, err
		}

		err = txData.VerifyMoveProof(publicWitness)
		if err != nil {
			err = fmt.Errorf("error with verifying circuit for send energy tx from planet with location hash %s: %w", txData.LocationHashFrom, err)
			log.Error().Err(err).Msg("")
			return result, err
		}

		// TODO: make this atomic
		// 2f. POST-CONDITION: Destination planet is created if it doesn't exist before
		var planetToId cardinal.EntityID
		planetToEntity, ok := comp.LoadPlanetComponent(txData.LocationHashTo)
		planetTo := planetToEntity.Component
		if ok == false {
			log.Debug().Msgf("Destination planet at %s does not exist, creating now", txData.LocationHashTo)
			// Create the planet entity if the planet is unclaimed (doesn't exist in index)
			planetToId, err = cardinal.Create(wCtx, comp.PlanetComponent{})
			if err != nil {
				err = fmt.Errorf("failed to create planet with id %d: %w", planetToId, err)
				log.Error().Err(err).Msg("")
				return result, err
			}

			// We set the owner persona tag to "" because the planet is not owned by anyone yet
			// The owner persona tag will be set when the ship arrives
			lastUpdateRefillAge := new(decimal.Big).Quo(utils.StrToDec(planetToStats.EnergyDefault), utils.StrToDec(planetToStats.EnergyMax))
			spaceArea := utils.SpaceAreaToInt(utils.GetSpaceArea(txData.PerlinTo))
			planetReceipt := tx.PlanetReceipt{
				Level:               planetToStats.Level,
				LocationHash:        txData.LocationHashTo,
				OwnerPersonaTag:     "",
				EnergyCurrent:       planetToStats.EnergyDefault,
				EnergyMax:           planetToStats.EnergyMax,
				EnergyRefill:        planetToStats.EnergyRefill,
				Defense:             planetToStats.Defense,
				Range:               planetToStats.Range,
				Speed:               planetToStats.Speed,
				LastUpdateRefillAge: utils.DecToStr(lastUpdateRefillAge),
				LastUpdateTick:      strconv.FormatUint(wCtx.CurrentTick(), 10),
				SpaceArea:           spaceArea,
			}

			newPlanet := convertPlanetReceiptToComp(planetReceipt)
			planetTo = newPlanet

			err = planetTo.Set(wCtx, planetToId)
			if err != nil {
				err = fmt.Errorf("failed to set stats for planet with id %d: %w", planetToId, err)
				log.Error().Err(err).Msg("")
				return result, err
			}
			log.Debug().Msgf("Created destination planet at %s with stats: %+v", txData.LocationHashTo, planetReceipt)
			result.NewPlanet = planetReceipt
		}

		// 2fi. PRE-CONDITION: Verify that the ship has enough energy to reach the destination planet with energy to spare
		energyOnEmbark := utils.EnergyOnEmbark(utils.Int64ToDec(txData.Energy), planetFrom.EnergyMax, utils.Int64ToDec(txData.MaxDistance), planetFrom.Range)
		enoughEnergyForFriendlyPlant := (planetFrom.OwnerPersonaTag == planetTo.OwnerPersonaTag) && (utils.LessThanOrEqual(utils.EnergyOnArrivalAtFriendlyPlanet(energyOnEmbark), decimal.New(0, 0)))
		if enoughEnergyForFriendlyPlant {
			err = fmt.Errorf("ship did not have enough energy to arrive at friendly planet")
			log.Error().Err(err).Msg("")
			return result, err
		}

		enoughEnergyForEnemyPlanet := (planetFrom.OwnerPersonaTag != planetTo.OwnerPersonaTag) && (utils.LessThanOrEqual(utils.EnergyAfterDefenseDebuff(energyOnEmbark, planetTo.Defense), decimal.New(0, 0)))
		if enoughEnergyForEnemyPlanet {
			err = fmt.Errorf("ship did not have enough energy to arrive at enemy planet")
			log.Error().Err(err).Msg("")
			return result, err
		}

		// 2f. POST-CONDITION: Ship entity is created
		shipId, err := cardinal.Create(wCtx, comp.ShipComponent{})
		if err != nil {
			err = fmt.Errorf("failed to create slip with id %d: %w", shipId, err)
			log.Error().Err(err).Msg("")
			return result, err
		}

		shipReceipt := tx.ShipReceipt{
			Id:               uint64(shipId),
			OwnerPersonaTag:  txSig.PersonaTag,
			LocationHashFrom: txData.LocationHashFrom,
			LocationHashTo:   txData.LocationHashTo,
			TickStart:        int64(wCtx.CurrentTick()),
			TickArrive:       utils.ShipArrivalTick(utils.Int64ToDec(txData.MaxDistance), utils.ScaleDownByTickRate(planetFrom.Speed), int64(wCtx.CurrentTick())),
			EnergyOnEmbark:   utils.DecToStr(energyOnEmbark),
		}

		newShipComp := convertShipReceiptToComp(shipReceipt)
		err = newShipComp.Set(wCtx, shipId)
		if err != nil {
			err = fmt.Errorf("failed to set stats for ship with id %d: %w", shipId, err)
			log.Error().Err(err).Msg("")
			return result, err
		}
		log.Debug().Msgf("Ship was created successfully: %+v", shipReceipt)

		planetFrom.LastUpdateTick = utils.IntToDec(int(wCtx.CurrentTick()))
		planetFrom.EnergyCurrent = new(decimal.Big).Sub(planetFrom.EnergyCurrent, utils.Int64ToDec(txData.Energy))
		planetFrom.LastUpdateRefillAge = utils.InvEnergyCurve(utils.InvEnergyCurve(utils.Saturate(new(decimal.Big).Quo(planetFrom.EnergyCurrent, planetFrom.EnergyMax))))
		log.Debug().Msgf("Slashed energy of planet with location hash %s to %s after sending ship", planetFrom.LocationHash, utils.DecToStr(planetFrom.EnergyCurrent))

		err = planetFrom.Set(wCtx, planetFromEntity.EntityId)
		if err != nil {
			err = fmt.Errorf("failed to set stats for planet with id %d: %w", planetFromEntity.EntityId, err)
			log.Error().Err(err).Msg("")
			return result, err
		}

		result.SentShip = shipReceipt
		result.NewSenderEnergy = utils.DecToStr(planetFrom.EnergyCurrent)
		return result, nil
	})

	return nil
}
