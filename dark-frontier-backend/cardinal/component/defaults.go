package component

import (
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"pkg.world.dev/world-engine/cardinal"
)

type DefaultsComponent struct {
	WorldConstants       game.WorldConstant
	NebulaSpaceConstants game.SpaceConstant
	SafeSpaceConstants   game.SpaceConstant
	DeepSpaceConstants   game.SpaceConstant
	Level0PlanetStats    game.PlanetLevelStats
	Level1PlanetStats    game.PlanetLevelStats
	Level2PlanetStats    game.PlanetLevelStats
	Level3PlanetStats    game.PlanetLevelStats
	Level4PlanetStats    game.PlanetLevelStats
	Level5PlanetStats    game.PlanetLevelStats
	Level6PlanetStats    game.PlanetLevelStats
}

func (DefaultsComponent) Name() string {
	return "DefaultsComponent"
}

func LoadDefaultsComponent(wCtx cardinal.WorldContext) (dc *DefaultsComponent, err error) {
	dc, _, err = GetDefaultsComponent(wCtx)
	if err != nil {
		return nil, err
	}

	// Found existing defaults component, set game constants using it
	game.WorldConstants = dc.WorldConstants
	game.NebulaSpaceConstants = dc.NebulaSpaceConstants
	game.SafeSpaceConstants = dc.SafeSpaceConstants
	game.DeepSpaceConstants = dc.DeepSpaceConstants
	game.PlanetLevel0Stats = dc.Level0PlanetStats
	game.PlanetLevel1Stats = dc.Level1PlanetStats
	game.PlanetLevel2Stats = dc.Level2PlanetStats
	game.PlanetLevel3Stats = dc.Level3PlanetStats
	game.PlanetLevel4Stats = dc.Level4PlanetStats
	game.PlanetLevel5Stats = dc.Level5PlanetStats
	game.PlanetLevel6Stats = dc.Level6PlanetStats
	game.SpaceConstants = [3]*game.SpaceConstant{
		&game.NebulaSpaceConstants,
		&game.SafeSpaceConstants,
		&game.DeepSpaceConstants,
	}
	game.BasePlanetLevelStats = [11]*game.PlanetLevelStats{
		&game.PlanetLevel0Stats,
		&game.PlanetLevel1Stats,
		&game.PlanetLevel2Stats,
		&game.PlanetLevel3Stats,
		&game.PlanetLevel4Stats,
		&game.PlanetLevel5Stats,
		&game.PlanetLevel6Stats,
		&game.PlanetLevel7Stats,
		&game.PlanetLevel8Stats,
		&game.PlanetLevel9Stats,
		&game.PlanetLevel10Stats,
	}

	return dc, nil
}

func BuildAndSetDefaultsComponent(wCtx cardinal.WorldContext) error {
	dc := DefaultsComponent{
		WorldConstants:       game.WorldConstants,
		NebulaSpaceConstants: game.NebulaSpaceConstants,
		SafeSpaceConstants:   game.SafeSpaceConstants,
		DeepSpaceConstants:   game.DeepSpaceConstants,
		Level0PlanetStats:    game.PlanetLevel0Stats,
		Level1PlanetStats:    game.PlanetLevel1Stats,
		Level2PlanetStats:    game.PlanetLevel2Stats,
		Level3PlanetStats:    game.PlanetLevel3Stats,
		Level4PlanetStats:    game.PlanetLevel4Stats,
		Level5PlanetStats:    game.PlanetLevel5Stats,
		Level6PlanetStats:    game.PlanetLevel6Stats,
	}
	id, err := cardinal.Create(wCtx, DefaultsComponent{})
	if err != nil {
		wCtx.Logger().Error().Err(err).Msg("Failed to create entity with DefaultsComponent")
		return err
	}
	err = setDefaultsComponent(wCtx, dc, id)
	if err != nil {
		return err
	}
	return nil
}

func UpdateSpaceDefaults(wCtx cardinal.WorldContext, spaceArea int64, newSpaceConstants game.SpaceConstant) error {
	dc, id, err := GetDefaultsComponent(wCtx)
	if err != nil {
		return err
	}
	switch spaceArea {
	case 0:
		dc.NebulaSpaceConstants = newSpaceConstants
		setDefaultsComponent(wCtx, *dc, id)
	case 1:
		dc.SafeSpaceConstants = newSpaceConstants
		setDefaultsComponent(wCtx, *dc, id)
	case 2:
		dc.DeepSpaceConstants = newSpaceConstants
		setDefaultsComponent(wCtx, *dc, id)
	default:
		wCtx.Logger().Error().Msg("Received invalid space area integer in UpdateSpaceDefaults()")
	}

	return nil
}

func UpdateLevelDefaults(wCtx cardinal.WorldContext, level int64, newLevelConstants game.PlanetLevelStats) error {
	dc, id, err := GetDefaultsComponent(wCtx)
	if err != nil {
		return err
	}
	switch level {
	case 0:
		dc.Level0PlanetStats = newLevelConstants
		setDefaultsComponent(wCtx, *dc, id)
	case 1:
		dc.Level1PlanetStats = newLevelConstants
		setDefaultsComponent(wCtx, *dc, id)
	case 2:
		dc.Level2PlanetStats = newLevelConstants
		setDefaultsComponent(wCtx, *dc, id)
	case 3:
		dc.Level3PlanetStats = newLevelConstants
		setDefaultsComponent(wCtx, *dc, id)
	case 4:
		dc.Level4PlanetStats = newLevelConstants
		setDefaultsComponent(wCtx, *dc, id)
	case 5:
		dc.Level5PlanetStats = newLevelConstants
		setDefaultsComponent(wCtx, *dc, id)
	case 6:
		dc.Level6PlanetStats = newLevelConstants
		setDefaultsComponent(wCtx, *dc, id)
	default:
		wCtx.Logger().Error().Msg("Received invalid space area integer in UpdateLevelDefaults()")
	}

	return nil
}

func setDefaultsComponent(wCtx cardinal.WorldContext, dc DefaultsComponent, id cardinal.EntityID) error {
	err := cardinal.SetComponent[DefaultsComponent](wCtx, id, &dc)
	if err != nil {
		wCtx.Logger().Error().Err(err).Msg("Failed to set DefaultsComponent")
		return err
	}
	return nil
}

func GetDefaultsComponent(wCtx cardinal.WorldContext) (dc *DefaultsComponent, id cardinal.EntityID, err error) {
	search, err := wCtx.NewSearch(cardinal.Exact(DefaultsComponent{}))
	if err != nil {
		return nil, cardinal.EntityID(0), err
	}
	id, err = search.First(wCtx)
	if err != nil {
		return nil, cardinal.EntityID(0), err
	}
	comp, err := cardinal.GetComponent[DefaultsComponent](wCtx, id)
	if err != nil {
		return nil, cardinal.EntityID(0), err
	}
	dc = comp

	return dc, id, nil
}
