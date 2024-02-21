package system

import (
	"context"
	"fmt"
	comp "github.com/argus-labs/darkfrontier-backend/cardinal/component"
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"github.com/argus-labs/darkfrontier-backend/cardinal/utils"
	"github.com/ericlagergren/decimal"
	"pkg.world.dev/world-engine/cardinal"
	"strconv"
)

func ShipArriveSystem(wCtx cardinal.WorldContext) error {
	log := wCtx.Logger()

	// Check that the game timer is not over, if it is, exit
	err := checkTimer(wCtx)
	if err != nil {
		log.Debug().Msg(err.Error())
		return nil
	}

	// 1. For each ships
	comp.ShipIndex.Range(func(key, value interface{}) bool {
		// Type assertion to get the actual types of key and value
		shipId, ok1 := key.(cardinal.EntityID)
		ship, ok2 := value.(comp.ShipComponent)

		// Check if the type assertion was successful
		if !ok1 || !ok2 {
			wCtx.Logger().Info().Msg("Found incorrect type in key or value of ShipIndex sync.Map")
			return true
		}

		// 1a. PRE-CONDITION: Verify that the current tick is passed the arrival tick
		if ship.TickArrive > int64(wCtx.CurrentTick()) {
			return true
		}
		log.Debug().Msgf("Starting to process ship arrival for ship with planetFrom: %s, planetTo: %s, energyOnEmbark: %s", ship.LocationHashFrom, ship.LocationHashTo, utils.DecToStr(ship.EnergyOnEmbark))

		// 1b. PRE-CONDITION: Check that the planet already exists in ECS
		planetToEntity, ok := comp.LoadPlanetComponent(ship.LocationHashTo)
		if ok == false {
			log.Error().Msgf("tried to send a ship to a non-existing planet %s", ship.LocationHashTo)
			return true
		}
		planetTo := planetToEntity.Component
		planetToId := planetToEntity.EntityId

		// 1i. PRE-CONDITION: Check that the planet is claimed (has an owner)
		if planetTo.OwnerPersonaTag != "" {
			// Apply lazy energy refill
			log.Debug().Msgf("Applying lazy energy refill to planet: %s", planetTo.LocationHash)
			normalizedRefillAge := utils.NormalizedRefillAge(planetTo.LastUpdateRefillAge, planetTo.LastUpdateTick, utils.IntToDec(int(wCtx.CurrentTick())), utils.ScaleUpByTickRate(planetTo.EnergyRefill))
			planetTo.EnergyCurrent = utils.EnergyLevel(planetTo.EnergyMax, normalizedRefillAge)
			planetTo.LastUpdateTick = utils.IntToDec(int(wCtx.CurrentTick()))
			planetTo.LastUpdateRefillAge = normalizedRefillAge
			log.Debug().Msgf("Updated energy of planet with location hash %s to %s", planetTo.LocationHash, utils.DecToStr(planetTo.EnergyCurrent))
		}

		var shipEnergyOnArrival *decimal.Big
		if planetTo.OwnerPersonaTag == ship.OwnerPersonaTag {
			shipEnergyOnArrival = utils.EnergyOnArrivalAtFriendlyPlanet(ship.EnergyOnEmbark)
		} else {
			shipEnergyOnArrival = utils.EnergyAfterDefenseDebuff(ship.EnergyOnEmbark, planetTo.Defense)
		}
		log.Debug().Msgf("Ship energy after arrival: %s", utils.DecToStr(shipEnergyOnArrival))

		// 1bii. PRE-CONDITION: Verify that the ship's energy is positive so it doesn't increase the planet's energy
		if utils.LessThan(decimal.New(0, 0), shipEnergyOnArrival) {
			if planetTo.OwnerPersonaTag == ship.OwnerPersonaTag {
				// Handle the case where the planet is owned by the player
				e := new(decimal.Big).Add(shipEnergyOnArrival, planetTo.EnergyCurrent)

				// Clamp the energy to the planet max energy
				planetTo.EnergyCurrent = utils.DecMin(e, planetTo.EnergyMax)
				log.Debug().Msgf("Ship is friendly, setting planetTo energy to %s", utils.DecToStr(planetTo.EnergyCurrent))

				err := planetTo.Set(wCtx, planetToId)
				if err != nil {
					log.Error().Err(err).Msg("Failed to set planet component after friendly ship arrival")
					return true
				}
			} else {
				// Handle the case where the planet is owned by another player
				// Check that planetTo's energy is below 0 (it's been conquered)
				postAttackEnergy := new(decimal.Big).Sub(planetTo.EnergyCurrent, shipEnergyOnArrival)
				log.Debug().Msgf("Ship was not friendly, postAttackEnergy of planetTo is %s", utils.DecToStr(postAttackEnergy))

				isConquered := utils.LessThan(postAttackEnergy, utils.IntToDec(0))
				if isConquered {
					previousOwner := planetTo.OwnerPersonaTag
					planetTo.OwnerPersonaTag = ship.OwnerPersonaTag

					// Reverse the application of the planet's defense before applying the remaining energy to the planet
					reverseDefensePostAttackEnergy := new(decimal.Big).Quo(new(decimal.Big).Mul(postAttackEnergy, planetTo.Defense), utils.StrToDec("100"))
					// Also, clamp the energy to the planet max energy
					planetTo.EnergyCurrent = utils.DecMin(new(decimal.Big).Abs(reverseDefensePostAttackEnergy), planetTo.EnergyMax)

					basePlanetScore, err := strconv.Atoi(game.BasePlanetLevelStats[int(planetTo.Level)].Score)
					if err != nil {
						err = fmt.Errorf("failed to convert string to int, error: %w", err)
						log.Error().Err(err).Msg("")
						return true
					}
					spaceAreaScoreMultiplier, err := strconv.Atoi(utils.GetSpaceArea(planetTo.SpaceArea).ScoreMultiplier)
					if err != nil {
						err = fmt.Errorf("failed to convert string to int, error: %w", err)
						log.Error().Err(err).Msg("")
						return true
					}
					score := basePlanetScore * spaceAreaScoreMultiplier

					// Decrement score of player that lost the planet
					err = game.DecrementScore(context.Background(), previousOwner, score)
					if err != nil {
						log.Error().Msgf("Failed to decrement score for persona tag %s: %v", previousOwner, err)
						return true
					}

					// Increment score of player that conquered the planet
					err = game.IncrementScore(context.Background(), ship.OwnerPersonaTag, score)
					if err != nil {
						log.Error().Msgf("Failed to increment score for persona tag %s: %v", ship.OwnerPersonaTag, err)
						return true
					}

					log.Debug().Msgf("Planet %s was conquered, setting energy to %s", planetTo.LocationHash, utils.DecToStr(planetTo.EnergyCurrent))
				} else {
					// Handle the case where the planet is not conquered
					planetTo.EnergyCurrent = postAttackEnergy
					log.Debug().Msgf("Planet %s was not conquered, setting new energy to %s", planetTo.LocationHash, utils.DecToStr(planetTo.EnergyCurrent))
				}
			}

			planetTo.LastUpdateRefillAge = utils.InvEnergyCurve(utils.InvEnergyCurve(utils.Saturate(new(decimal.Big).Quo(planetTo.EnergyCurrent, planetTo.EnergyMax))))
			planetTo.LastUpdateTick = utils.IntToDec(int(wCtx.CurrentTick()))
			log.Debug().Msgf("Updated refill age after ship landing for the receiving planet %s", planetTo.LocationHash)

			err := planetTo.Set(wCtx, planetToId)
			if err != nil {
				log.Error().Err(err).Msg("Error updating planet component after ship arrive refill.")
				return true
			}
		}

		// 1c. POST-CONDITION: Delete the ship
		err := ship.Remove(wCtx, shipId)
		if err != nil {
			return true
		}
		return false
	})

	return nil
}
