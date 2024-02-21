package system

import (
	"fmt"
	"github.com/argus-labs/darkfrontier-backend/cardinal/component"
	"github.com/argus-labs/darkfrontier-backend/cardinal/tx"
	"github.com/argus-labs/darkfrontier-backend/cardinal/utils"
	"github.com/ericlagergren/decimal"
	"pkg.world.dev/world-engine/cardinal"
)

func DebugEnergyBoostSystem(wCtx cardinal.WorldContext) error {
	log := wCtx.Logger()

	// Check that the game timer is not over, if it is, exit
	err := checkTimer(wCtx)
	if err != nil {
		log.Debug().Msg(err.Error())
		return nil
	}

	tx.DebugEnergyBoost.Each(wCtx, func(t cardinal.TxData[tx.DebugEnergyBoostMsg]) (result tx.DebugEnergyBoostReply, err error) {
		txData := t.Msg()
		txSig := t.Tx()

		log.Debug().Msgf("Received payload to debug-energy-boost at location hash: %s", txData.LocationHash)

		// Check that location hash is valid
		if err = txData.Validate(); err != nil {
			log.Error().Err(err).Msg("")
			return result, err
		}

		// Check that planet exists
		planetEntity, ok := component.LoadPlanetComponent(txData.LocationHash)
		if ok == false {
			err = fmt.Errorf("planet at location hash %s does not exist", txData.LocationHash)
			log.Error().Err(err).Msg("")
			return result, err
		}
		planetId := planetEntity.EntityId
		planet := planetEntity.Component

		// Check that the planet owner is the tx sender
		if planet.OwnerPersonaTag != txSig.PersonaTag {
			err = fmt.Errorf("player with persona %s does not own planet with at location hash %s", txSig.PersonaTag, planet.LocationHash)
			log.Error().Err(err).Msg("")
			return result, err
		}

		// 1) Apply lazy energy refill
		log.Debug().Msgf("Applying lazy energy refill to planet: %s", planet.LocationHash)
		normalizedRefillAge := utils.NormalizedRefillAge(planet.LastUpdateRefillAge, planet.LastUpdateTick, utils.IntToDec(int(wCtx.CurrentTick())), planet.EnergyRefill)
		planet.EnergyCurrent = utils.EnergyLevel(planet.EnergyMax, normalizedRefillAge)
		planet.LastUpdateTick = utils.IntToDec(int(wCtx.CurrentTick()))
		planet.LastUpdateRefillAge = normalizedRefillAge
		log.Debug().Msgf("Updated energy of planet with location hash %s to %s", planet.LocationHash, utils.DecToStr(planet.EnergyCurrent))

		// 2) Calculate what a 25% energy boost would be and apply the increase
		energyBoost := new(decimal.Big).Mul(planet.EnergyMax, utils.StrToDec("0.25"))
		planet.EnergyCurrent = new(decimal.Big).Add(planet.EnergyCurrent, energyBoost)

		// 3) Update energy regen attributes to match this new boosted energy
		planet.LastUpdateRefillAge = utils.InvEnergyCurve(utils.InvEnergyCurve(utils.Saturate(new(decimal.Big).Quo(planet.EnergyCurrent, planet.EnergyMax))))
		planet.LastUpdateTick = utils.IntToDec(int(wCtx.CurrentTick()))
		err = planet.Set(wCtx, planetId)
		if err != nil {
			err = fmt.Errorf("failed to set stats for planet with id %d: %w", planetId, err)
			log.Error().Err(err).Msg("")
			return result, err
		}

		log.Debug().Msgf("Successfully applied energy boost to planet at location hash: %s", txData.LocationHash)
		return result, nil
	})

	return nil
}
