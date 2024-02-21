package system

import (
	"errors"
	"fmt"
	comp "github.com/argus-labs/darkfrontier-backend/cardinal/component"
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"github.com/argus-labs/darkfrontier-backend/cardinal/tx"
	"github.com/argus-labs/darkfrontier-backend/cardinal/utils"
	"pkg.world.dev/world-engine/cardinal"
	"strconv"
	"strings"
)

func checkTimer(wCtx cardinal.WorldContext) error {
	// InstanceTimer is in seconds, divide current tick by tick rate to get the number of seconds that have past,
	// if seconds past is greater than or equal to the InstanceTimer then we no longer accept new transaction
	if wCtx.CurrentTick()/uint64(game.WorldConstants.TickRate) >= uint64(game.WorldConstants.InstanceTimer) {
		return errors.New("timer has ran out, messages are no longer being accepted, game over")
	}
	return nil
}

func rebuildDefaultsAndComponentIndexes(wCtx cardinal.WorldContext) error {
	err := comp.RebuildPlanetIndex(wCtx)
	if err != nil {
		return fmt.Errorf("failed to rebuild planet index: %w", err)
	}

	err = comp.RebuildPlayerIndex(wCtx)
	if err != nil {
		return fmt.Errorf("failed to rebuild player index: %w", err)
	}

	err = comp.RebuildShipIndex(wCtx)
	if err != nil {
		return fmt.Errorf("failed to rebuild ship index: %w", err)
	}

	dc, err := comp.LoadDefaultsComponent(wCtx)
	if err != nil {
		wCtx.Logger().Info().Msg("DefaultsComponent did not exist, building now")
	}
	if dc == nil {
		err = comp.BuildAndSetDefaultsComponent(wCtx)
		if err != nil {
			return fmt.Errorf("failed to build and set DefaultsComponent %w", err)
		}
		wCtx.Logger().Info().Msg("Successfully built and set DefaultsComponent")
	}
	wCtx.Logger().Info().Msg("Successfully rebuilt all defaults and component indexes.")
	return nil
}

func convertPlanetReceiptToComp(pr tx.PlanetReceipt) comp.PlanetComponent {
	newPlanet := comp.PlanetComponent{
		Level:               pr.Level,
		LocationHash:        pr.LocationHash,
		OwnerPersonaTag:     pr.OwnerPersonaTag,
		EnergyCurrent:       utils.StrToDec(pr.EnergyCurrent),
		EnergyMax:           utils.StrToDec(pr.EnergyMax),
		EnergyRefill:        utils.StrToDec(pr.EnergyRefill),
		Defense:             utils.StrToDec(pr.Defense),
		Range:               utils.StrToDec(pr.Range),
		Speed:               utils.StrToDec(pr.Speed),
		LastUpdateRefillAge: utils.StrToDec(pr.LastUpdateRefillAge),
		LastUpdateTick:      utils.StrToDec(pr.LastUpdateTick),
		SpaceArea:           pr.SpaceArea,
	}
	return newPlanet
}

func convertShipReceiptToComp(sr tx.ShipReceipt) comp.ShipComponent {
	newShipComp := comp.ShipComponent{
		OwnerPersonaTag:  sr.OwnerPersonaTag,
		LocationHashFrom: sr.LocationHashFrom,
		LocationHashTo:   sr.LocationHashTo,
		TickStart:        sr.TickStart,
		TickArrive:       sr.TickArrive,
		EnergyOnEmbark:   utils.StrToDec(sr.EnergyOnEmbark),
	}
	return newShipComp
}

func findAndUpdatePlanetsBySpaceArea(wCtx cardinal.WorldContext, spaceArea int64, msg SpaceConstantsMsg) error {
	prevSpaceConstants := game.SpaceConstants[spaceArea]
	newSpaceConstants := game.SpaceConstant{
		Label:                   prevSpaceConstants.Label,
		PlanetSpawnThreshold:    prevSpaceConstants.PlanetSpawnThreshold,
		PlanetLevelThreshold:    prevSpaceConstants.PlanetLevelThreshold,
		StatBuffMultiplier:      msg.StatBuffMultiplier,
		DefenseDebuffMultiplier: msg.DefenseDebuffMultiplier,
		ScoreMultiplier:         msg.ScoreMultiplier,
	}

	// Loop over existing planets, find all planets in the given space area, calc and apply updates
	comp.PlanetIndex.Range(func(key any, value any) bool {
		planetEntity := value.(comp.PlanetEntity)
		if planetEntity.Component.SpaceArea == spaceArea {
			// Get new stats based on new space constants
			newStats := utils.GetSpaceAdjustedPlanetStats(*game.BasePlanetLevelStats[planetEntity.Component.Level], newSpaceConstants)

			// Update the planet component with new stats
			planetEntity.Component.EnergyMax = utils.StrToDec(newStats.EnergyMax)
			planetEntity.Component.EnergyRefill = utils.StrToDec(newStats.EnergyRefill)
			planetEntity.Component.Defense = utils.StrToDec(newStats.Defense)
			planetEntity.Component.Speed = utils.StrToDec(newStats.Speed)
			planetEntity.Component.Range = utils.StrToDec(newStats.Range)

			planetEntity.Component.Set(wCtx, planetEntity.EntityId)
		}
		return true
	})

	// Update the game constants
	*game.SpaceConstants[spaceArea] = newSpaceConstants

	// Update the respective SpaceConstant in the DefaultConstants component
	err := comp.UpdateSpaceDefaults(wCtx, spaceArea, newSpaceConstants)
	if err != nil {
		return err
	}
	return nil
}

func findAndUpdatePlanetsByLevel(wCtx cardinal.WorldContext, level int64, msg LevelConstantsMsg) error {
	newLevelConstants := game.PlanetLevelStats{
		Level:         level,
		EnergyDefault: msg.EnergyDefault,
		EnergyMax:     msg.EnergyMax,
		EnergyRefill:  msg.EnergyRefill,
		Range:         msg.Range,
		Speed:         msg.Speed,
		Defense:       msg.Defense,
		Score:         msg.Score,
	}

	// Loop over existing planets, find all planets with the given level, calc and apply updates
	comp.PlanetIndex.Range(func(key any, value any) bool {
		planetEntity := value.(comp.PlanetEntity)
		if planetEntity.Component.Level == level {
			// Get new stats based on new level constants
			newStats := utils.GetSpaceAdjustedPlanetStats(newLevelConstants, *game.SpaceConstants[planetEntity.Component.SpaceArea-1])

			// Update the planet component with new stats
			planetEntity.Component.EnergyMax = utils.StrToDec(newStats.EnergyMax)
			planetEntity.Component.EnergyRefill = utils.StrToDec(newStats.EnergyRefill)
			planetEntity.Component.Defense = utils.StrToDec(newStats.Defense)
			planetEntity.Component.Speed = utils.StrToDec(newStats.Speed)
			planetEntity.Component.Range = utils.StrToDec(newStats.Range)

			planetEntity.Component.Set(wCtx, planetEntity.EntityId)
		}
		return true
	})

	// Update the level constants
	*game.BasePlanetLevelStats[level] = newLevelConstants

	// Update the level constants in DefaultsComponent
	err := comp.UpdateLevelDefaults(wCtx, level, newLevelConstants)
	if err != nil {
		return err
	}
	return nil
}

func extractLevelNumber(constantName string) (int64, error) {
	// Assuming the constant name is in the format "Level[0-6]Constants"
	const prefix = "Level"
	const suffix = "Constants"

	if strings.HasPrefix(constantName, prefix) && strings.HasSuffix(constantName, suffix) {
		levelStr := strings.TrimPrefix(constantName, prefix)
		levelStr = strings.TrimSuffix(levelStr, suffix)

		level, err := strconv.Atoi(levelStr)
		if err != nil {
			return 0, fmt.Errorf("failed to extract level number from constant name: %v", err)
		}

		return int64(level), nil
	}

	return 0, errors.New("constant name does not follow the expected format")
}
