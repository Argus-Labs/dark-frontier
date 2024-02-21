package system

import (
	"context"
	"fmt"
	comp "github.com/argus-labs/darkfrontier-backend/cardinal/component"
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"github.com/argus-labs/darkfrontier-backend/cardinal/tx"
	"github.com/argus-labs/darkfrontier-backend/cardinal/utils"
	"github.com/ericlagergren/decimal"
	"pkg.world.dev/world-engine/cardinal"
	"strconv"
)

func DebugClaimPlanetSystem(wCtx cardinal.WorldContext) error {
	log := wCtx.Logger()

	// Check that the game timer is not over, if it is, exit
	err := checkTimer(wCtx)
	if err != nil {
		log.Debug().Msg(err.Error())
		return nil
	}

	tx.DebugClaimPlanet.Each(wCtx, func(t cardinal.TxData[tx.DebugClaimPlanetMsg]) (result tx.DebugClaimPlanetReply, err error) {
		txData := t.Msg()
		txSig := t.Tx()

		log.Debug().Msgf("Received payload to debug-claim-home-planet at location hash: %s", txData.LocationHash)

		// 1. PRE-CONDITION: Check that the LocationHash is well formatted
		if err = txData.Validate(); err != nil {
			log.Error().Err(err).Msg("")
			return result, err
		}

		// 2b. PRE-CONDITION: Check that the planet is not claimed
		// If the planet is claimed, then it will be in the index.
		// Therefore, we check that the planet is not in the index.
		planetEntity, ok := comp.LoadPlanetComponent(txData.LocationHash)
		if ok == true {
			err = fmt.Errorf("planet with location hash %s is already claimed", planetEntity.Component.LocationHash)
			log.Error().Err(err).Msg("")
			return result, err
		}

		// 2c. PRE-CONDITION: Check that location hash is a valid planet
		planetStats, err := utils.GetPlanetStatsByLocationHash(txData.LocationHash, txData.Perlin)
		if err != nil {
			err = fmt.Errorf("planet at location hash %s is not valid: %w", txData.LocationHash, err)
			log.Error().Err(err).Msg("")
			return result, err
		}

		// 2d. POST-CONDITION: Create a new planet with the player as owner
		id, err := cardinal.Create(wCtx, comp.PlanetComponent{})
		if err != nil {
			err = fmt.Errorf("failed to create and claim planet with id %d, error: %w", id, err)
			log.Error().Err(err).Msg("")
			return result, err
		}

		lastUpdateRefillAge := new(decimal.Big).Quo(utils.StrToDec(planetStats.EnergyDefault), utils.StrToDec(planetStats.EnergyMax))
		spaceArea := utils.SpaceAreaToInt(utils.GetSpaceArea(txData.Perlin))
		planetReceipt := tx.PlanetReceipt{
			Level:               planetStats.Level,
			LocationHash:        txData.LocationHash,
			OwnerPersonaTag:     txSig.PersonaTag,
			EnergyCurrent:       planetStats.EnergyDefault,
			EnergyMax:           planetStats.EnergyMax,
			EnergyRefill:        planetStats.EnergyRefill,
			Defense:             planetStats.Defense,
			Range:               planetStats.Range,
			Speed:               planetStats.Speed,
			LastUpdateRefillAge: utils.DecToStr(lastUpdateRefillAge),
			LastUpdateTick:      strconv.FormatUint(wCtx.CurrentTick(), 10),
			SpaceArea:           spaceArea,
		}

		homePlanetComp := convertPlanetReceiptToComp(planetReceipt)
		err = homePlanetComp.Set(wCtx, id)
		if err != nil {
			err = fmt.Errorf("failed to set stats for claimed home planet %d, error: %w", id, err)
			log.Error().Err(err).Msg("")
			return result, err
		}

		// 2e. POST-CONDITION: Create a new player entity
		id, err = cardinal.Create(wCtx, comp.PlayerComponent{})
		if err != nil {
			err = fmt.Errorf("failed to create player component with id %d, error: %w", id, err)
			log.Error().Err(err).Msg("")
			return result, err
		}

		newPlayer := comp.PlayerComponent{
			PersonaTag:            txSig.PersonaTag,
			HaveClaimedHomePlanet: true,
		}
		err = newPlayer.Set(wCtx, id)
		if err != nil {
			err = fmt.Errorf("failed to set attributes for newly created player with id %d, error: %w", id, err)
			log.Error().Err(err).Msg("")
			return result, err
		}

		// Add player to leaderboard with the score of a Level 0 planet
		basePlanetScore, err := strconv.Atoi(game.BasePlanetLevelStats[int(homePlanetComp.Level)].Score)
		if err != nil {
			err = fmt.Errorf("failed to convert string to int, error: %w", err)
			log.Error().Err(err).Msg("")
			return result, err
		}

		score := basePlanetScore * int(homePlanetComp.SpaceArea)
		err = game.AddPlayerToLeaderboard(context.Background(), game.Player{
			PersonaTag: txSig.PersonaTag,
			Score:      score,
		})
		if err != nil {
			err = fmt.Errorf("failed to add player with id %d to leaderboard, error: %w", id, err)
			log.Error().Err(err).Msg("")
			return result, err
		}

		log.Debug().Msgf("Successfully created player with persona %s with home planet at location hash %s", newPlayer.PersonaTag, homePlanetComp.LocationHash)

		result = tx.DebugClaimPlanetReply{
			ClaimedPlanet: planetReceipt,
			CreatedPlayer: newPlayer,
		}
		return result, nil
	})

	return nil
}
