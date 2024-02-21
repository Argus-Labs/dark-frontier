package component

import (
	"pkg.world.dev/world-engine/cardinal"
	"sync"
)

type PlayerComponent struct {
	PersonaTag            string `json:"personaTag"`
	HaveClaimedHomePlanet bool   `json:"haveClaimedHomePlanet"`
}

func (PlayerComponent) Name() string {
	return "PlayerComponent"
}

var PlayerIndex sync.Map

func (player PlayerComponent) Set(wCtx cardinal.WorldContext, id cardinal.EntityID) error {
	err := cardinal.SetComponent[PlayerComponent](wCtx, id, &player)
	if err != nil {
		wCtx.Logger().Error().Err(err).Msg("Failed to set player component")
		return err
	}

	PlayerIndex.Store(player.PersonaTag, player)
	return nil
}

func LoadPlayerComponent(key string) (PlayerComponent, bool) {
	value, ok := PlayerIndex.Load(key)
	if !ok {
		return PlayerComponent{}, false
	}

	player, ok := value.(PlayerComponent)
	if !ok {
		return PlayerComponent{}, false
	}

	return player, true
}

func RebuildPlayerIndex(wCtx cardinal.WorldContext) error {
	search, err := wCtx.NewSearch(cardinal.Exact(PlayerComponent{}))
	if err != nil {
		wCtx.Logger().Error().Err(err).Msg("Error performing search for player component in RebuildPlayerIndex()")
		return err
	}
	search.Each(wCtx, func(id cardinal.EntityID) bool {
		player, err := cardinal.GetComponent[PlayerComponent](wCtx, id)
		if err != nil {
			return true
		}
		PlayerIndex.Store(player.PersonaTag, *player)
		return true
	})
	return nil
}
