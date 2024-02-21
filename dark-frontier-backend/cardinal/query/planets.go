package query

import (
	"github.com/argus-labs/darkfrontier-backend/cardinal/component"
	"github.com/argus-labs/darkfrontier-backend/cardinal/utils"
	"pkg.world.dev/world-engine/cardinal"
)

type EnergyTransfer struct {
	TransferId          uint64 `json:"transferId"`
	PlanetToHash        string `json:"planetToHash"`
	PlanetFromHash      string `json:"planetFromHash"`
	PercentCompletion   int64  `json:"percentCompletion"`
	EnergyOnEmbark      string `json:"energyOnEmbark"`
	OwnerPersonaTag     string `json:"ownerPersonaTag"`
	TravelTimeInSeconds int64  `json:"travelTimeInSeconds"`
}

type PlanetData struct {
	Level               int64            `json:"level"`
	LocationHash        string           `json:"locationHash"`
	OwnerPersonaTag     string           `json:"ownerPersonaTag"`
	EnergyCurrent       string           `json:"energyCurrent"`
	EnergyMax           string           `json:"energyMax"`
	EnergyRefill        string           `json:"energyRefill"`
	Defense             string           `json:"defense"`
	Range               string           `json:"range"`
	Speed               string           `json:"speed"`
	LastUpdateRefillAge string           `json:"lastUpdateRefillAge"`
	LastUpdateTick      string           `json:"lastUpdateTick"`
	EnergyTransfers     []EnergyTransfer `json:"energyTransfers"`
}

type PlanetsMsg struct {
	PlanetsList []string `json:"planetsList"`
}

type PlanetsReply struct {
	Planets []PlanetData `json:"planets"`
}

func Planets(wCtx cardinal.WorldContext, req *PlanetsMsg) (*PlanetsReply, error) {
	foundPlanets := make([]PlanetData, 0, len(req.PlanetsList))
	locationHashLookup := make(map[string]int, len(req.PlanetsList))
	for i, locationHash := range req.PlanetsList {
		locationHashLookup[locationHash] = i
	}

	// Map of location hash -> list of associated energy transfers
	energyTransferLookup := make(map[string][]EnergyTransfer)

	// Loop through ship index and find all ships that are incoming or outgoing to a planet in
	// the list of planets requested
	component.ShipIndex.Range(func(key, value interface{}) bool {
		// Type assertion to get the actual types of key and value
		shipId, ok1 := key.(cardinal.EntityID)
		ship, ok2 := value.(component.ShipComponent)

		// Check if the type assertion was successful
		if !ok1 || !ok2 {
			wCtx.Logger().Info().Msg("Found incorrect type in key or value of ShipIndex sync.Map")
			return true
		}
		completion := calculatePercentCompleted(ship.TickStart, int64(wCtx.CurrentTick()), ship.TickArrive)
		if _, exists := locationHashLookup[ship.LocationHashTo]; exists {
			energyTransfer := EnergyTransfer{
				TransferId:          uint64(shipId),
				PlanetToHash:        ship.LocationHashTo,
				PlanetFromHash:      ship.LocationHashFrom,
				PercentCompletion:   completion,
				EnergyOnEmbark:      utils.DecToStr(ship.EnergyOnEmbark),
				OwnerPersonaTag:     ship.OwnerPersonaTag,
				TravelTimeInSeconds: utils.ScaleDownByTickRateInt(ship.TickArrive - ship.TickStart),
			}
			energyTransferLookup[ship.LocationHashTo] = append(energyTransferLookup[ship.LocationHashTo], energyTransfer)
		}
		if _, exists := locationHashLookup[ship.LocationHashFrom]; exists {
			energyTransfer := EnergyTransfer{
				TransferId:          uint64(shipId),
				PlanetToHash:        ship.LocationHashTo,
				PlanetFromHash:      ship.LocationHashFrom,
				PercentCompletion:   completion,
				EnergyOnEmbark:      utils.DecToStr(ship.EnergyOnEmbark),
				OwnerPersonaTag:     ship.OwnerPersonaTag,
				TravelTimeInSeconds: utils.ScaleDownByTickRateInt(ship.TickArrive - ship.TickStart),
			}
			energyTransferLookup[ship.LocationHashFrom] = append(energyTransferLookup[ship.LocationHashFrom], energyTransfer)
		}
		return true
	})

	// Loop through the planet index and find all planets that were requested
	component.PlanetIndex.Range(func(key, value interface{}) bool {
		// Type assertion to get the actual types of key and value
		_, ok1 := key.(string)
		planetEntity, ok2 := value.(component.PlanetEntity)
		if !ok1 || !ok2 {
			wCtx.Logger().Info().Msg("Found incorrect type in key or value of PlanetIndex sync.Map")
			return true
		}
		planetComp := planetEntity.Component
		if _, exists := locationHashLookup[planetComp.LocationHash]; exists {
			planetData := PlanetData{
				Level:               planetComp.Level,
				LocationHash:        planetComp.LocationHash,
				OwnerPersonaTag:     planetComp.OwnerPersonaTag,
				EnergyCurrent:       utils.DecToStr(planetComp.EnergyCurrent),
				EnergyMax:           utils.DecToStr(planetComp.EnergyMax),
				EnergyRefill:        utils.DecToStr(planetComp.EnergyRefill),
				Defense:             utils.DecToStr(planetComp.Defense),
				Range:               utils.DecToStr(planetComp.Range),
				Speed:               utils.DecToStr(planetComp.Speed),
				LastUpdateRefillAge: utils.DecToStr(planetComp.LastUpdateRefillAge),
				LastUpdateTick:      utils.DecToStr(planetComp.LastUpdateTick),
				EnergyTransfers:     energyTransferLookup[planetComp.LocationHash],
			}
			foundPlanets = append(foundPlanets, planetData)
		}
		return true
	})

	return &PlanetsReply{foundPlanets}, nil
}

func calculatePercentCompleted(startTick, currentTick, arrivalTick int64) int64 {
	distanceInTicks := arrivalTick - startTick
	distanceTravelledInTicks := currentTick - startTick
	if distanceInTicks == 0 { // Avoid dividing by zero
		return 0
	}
	percentCompletion := (distanceTravelledInTicks * 100) / distanceInTicks

	return percentCompletion
}
