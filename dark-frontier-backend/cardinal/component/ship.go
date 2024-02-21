package component

import (
	"github.com/ericlagergren/decimal"
	"pkg.world.dev/world-engine/cardinal"
	"sync"
)

type ShipComponent struct {
	OwnerPersonaTag  string
	LocationHashFrom string
	LocationHashTo   string
	TickStart        int64
	TickArrive       int64
	EnergyOnEmbark   *decimal.Big
}

func (ShipComponent) Name() string {
	return "ShipComponent"
}

var ShipIndex sync.Map

func (ship ShipComponent) Set(wCtx cardinal.WorldContext, id cardinal.EntityID) error {
	err := cardinal.SetComponent[ShipComponent](wCtx, id, &ship)
	if err != nil {
		wCtx.Logger().Error().Err(err).Msg("Failed to set ship component")
		return err
	}

	ShipIndex.Store(id, ship)
	return nil
}

func (ship ShipComponent) Remove(wCtx cardinal.WorldContext, id cardinal.EntityID) error {
	err := cardinal.Remove(wCtx, id)
	if err != nil {
		return err
	}

	ShipIndex.Delete(id)
	return nil
}

func RebuildShipIndex(wCtx cardinal.WorldContext) error {
	search, err := wCtx.NewSearch(cardinal.Exact(ShipComponent{}))
	if err != nil {
		wCtx.Logger().Error().Err(err).Msg("Error performing search for ship component in RebuildShipIndex()")
		return err
	}
	search.Each(wCtx, func(id cardinal.EntityID) bool {
		ship, err := cardinal.GetComponent[ShipComponent](wCtx, id)
		if err != nil {
			return true
		}
		ShipIndex.Store(id, *ship)
		return true
	})
	return nil
}
