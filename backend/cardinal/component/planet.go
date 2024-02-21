package component

import (
	"github.com/ericlagergren/decimal"
	"pkg.world.dev/world-engine/cardinal"
	"sync"
)

type PlanetComponent struct {
	Level               int64        `json:"level"`
	LocationHash        string       `json:"locationHash"`
	OwnerPersonaTag     string       `json:"ownerPersonaTag"`
	EnergyCurrent       *decimal.Big `json:"energyCurrent"`
	EnergyMax           *decimal.Big `json:"energyMax"`
	EnergyRefill        *decimal.Big `json:"energyRefill"`
	Defense             *decimal.Big `json:"defense"`
	Range               *decimal.Big `json:"range"`
	Speed               *decimal.Big `json:"speed"`
	LastUpdateRefillAge *decimal.Big `json:"lastUpdateRefillAge"`
	LastUpdateTick      *decimal.Big `json:"lastUpdateTick"`
	SpaceArea           int64        `json:"spaceArea"`
}

func (PlanetComponent) Name() string {
	return "PlanetComponent"
}

type PlanetEntity struct {
	Component PlanetComponent
	EntityId  cardinal.EntityID
}

var PlanetIndex sync.Map

func (planet PlanetComponent) Set(wCtx cardinal.WorldContext, id cardinal.EntityID) error {
	err := cardinal.SetComponent[PlanetComponent](wCtx, id, &planet)
	if err != nil {
		wCtx.Logger().Error().Err(err).Msg("Failed to set planet component")
		return err
	}

	PlanetIndex.Store(planet.LocationHash, PlanetEntity{
		Component: planet,
		EntityId:  id,
	})
	return nil
}

func LoadPlanetComponent(key string) (PlanetEntity, bool) {
	value, ok := PlanetIndex.Load(key)
	if !ok {
		return PlanetEntity{}, false
	}

	planet, ok := value.(PlanetEntity)
	if !ok {
		return PlanetEntity{}, false
	}

	return planet, true
}

func RebuildPlanetIndex(wCtx cardinal.WorldContext) error {
	search, err := wCtx.NewSearch(cardinal.Exact(PlanetComponent{}))
	if err != nil {
		wCtx.Logger().Error().Err(err).Msg("Error performing search for planet component in RebuildPlanetIndex()")
		return err
	}
	search.Each(wCtx, func(id cardinal.EntityID) bool {
		planet, err := cardinal.GetComponent[PlanetComponent](wCtx, id)
		if err != nil {
			return true
		}
		PlanetIndex.Store(planet.LocationHash, PlanetEntity{
			Component: *planet,
			EntityId:  id,
		})
		return true
	})
	return nil
}
