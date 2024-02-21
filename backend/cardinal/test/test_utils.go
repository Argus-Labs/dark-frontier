package utils

import (
	"errors"
	"fmt"
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"github.com/argus-labs/darkfrontier-backend/cardinal/keys"
	"github.com/argus-labs/darkfrontier-backend/cardinal/query"
	tx "github.com/argus-labs/darkfrontier-backend/cardinal/tx"
	"github.com/argus-labs/darkfrontier-backend/circuit/initialize"
	"github.com/argus-labs/darkfrontier-backend/circuit/move"
	"github.com/consensys/gnark-crypto/ecc"
	"github.com/consensys/gnark/backend/groth16"
	"github.com/consensys/gnark/frontend"
	"github.com/consensys/gnark/frontend/cs/r1cs"
	"github.com/ericlagergren/decimal"
	"github.com/redis/go-redis/v9"
	"github.com/rs/zerolog"
	"github.com/rs/zerolog/log"
	"os"
	"pkg.world.dev/world-engine/cardinal"
	"pkg.world.dev/world-engine/cardinal/ecs"
	"pkg.world.dev/world-engine/cardinal/testutils"
	"strconv"
	"sync"
	"testing"
	"time"

	"github.com/argus-labs/darkfrontier-backend/cardinal/component"
	"github.com/argus-labs/darkfrontier-backend/cardinal/system"
	"github.com/argus-labs/darkfrontier-backend/cardinal/utils"
)

// Miscellaneous test utilities
func ScaffoldTestWorld(t *testing.T) (*cardinal.World, func()) {
	zerolog.TimeFieldFormat = zerolog.TimeFormatUnix

	err := os.Setenv("TIMER", "1000000")
	if err != nil {
		log.Debug().Msgf("Failed to set env var TIMER: %s", err)
	}
	err = utils.SetConstantsFromEnv()
	if err != nil {
		log.Debug().Msgf("Failed to set instance timer: %s", err)
	}

	// NOTE: for an in-memory Redis in a unit test, use this NewECSWorldForTest(t).
	// avoid miniredis port conflict & relevant resources are automatically cleaned up at the completion of each test.
	tickRateInTime := time.Duration(1000/game.WorldConstants.TickRate) * time.Millisecond
	newWorld, doTick := testutils.MakeWorldAndTicker(
		t,
		cardinal.WithReceiptHistorySize(5000),
		cardinal.WithTickChannel(time.Tick(tickRateInTime)),
	)

	// Register components
	// NOTE: You must register your components here,
	// otherwise it will show an error when you try to use them in a system.
	utils.Must(cardinal.RegisterComponent[component.PlayerComponent](newWorld))
	utils.Must(cardinal.RegisterComponent[component.PlanetComponent](newWorld))
	utils.Must(cardinal.RegisterComponent[component.ShipComponent](newWorld))
	utils.Must(cardinal.RegisterComponent[component.DefaultsComponent](newWorld))

	// Register transactions
	// NOTE: You must register your transactions here,
	// otherwise it will show an error when you try to use them in a system.
	utils.Must(cardinal.RegisterMessages(
		newWorld,
		tx.SendEnergy,
		tx.ClaimHomePlanet,
		tx.SetConstant,
	))

	// Register queries
	utils.Must(cardinal.RegisterQuery[query.ConstantMsg, query.ConstantReply](newWorld, "constant", query.Constants))
	utils.Must(cardinal.RegisterQuery[query.CurrentTickMsg, query.CurrentTickReply](newWorld, "current-tick", query.CurrentTick))
	utils.Must(cardinal.RegisterQuery[query.PlanetsMsg, query.PlanetsReply](newWorld, "planets", query.Planets))
	utils.Must(cardinal.RegisterQuery[query.PlayerRangeMsg, query.PlayerRangeReply](newWorld, "player-range", query.PlayerRange))
	utils.Must(cardinal.RegisterQuery[query.PlayerRankMsg, query.PlayerRankReply](newWorld, "player-rank", query.PlayerRank))

	// Register systems
	utils.Must(cardinal.RegisterSystems(
		newWorld,
		system.SendEnergySystem,
		system.ClaimHomePlanetSystem,
		system.ShipArriveSystem,
		system.SetConstantSystem,
	))

	// Wipe state of indexes in case they existed in a previous test run
	component.PlanetIndex = sync.Map{}
	component.ShipIndex = sync.Map{}
	component.PlayerIndex = sync.Map{}

	addr := os.Getenv("REDIS_ADDRESS")
	options := &redis.Options{
		Addr:     addr, // Redis server address
		Password: "",   // No password by default
		DB:       0,    // Default database
	}

	game.LeaderboardClient = redis.NewClient(options)

	go func() {
		err := newWorld.StartGame()
		if err != nil {
			panic("Failed to start game")
		}
	}()
	for !newWorld.IsGameRunning() {
		time.Sleep(1 * time.Second)
		fmt.Println("Waiting for game to start...")
	}

	return newWorld, doTick
}

func SetupCircuitsAndOverwriteConstants() {
	game.WorldConstants.Scale = 16
	game.WorldConstants.XMirror = 0
	game.WorldConstants.YMirror = 0
	game.WorldConstants.MiMCNumRounds = 110
	game.WorldConstants.PerlinNumRounds = 4
	game.WorldConstants.MiMCSeedWord = "7"
	game.WorldConstants.PerlinSeedWord = "7"
	game.WorldConstants.RadiusMax = 500

	var newInitCircuit initialize.InitCircuit
	newInitCircuit.PlanetHashKey = game.WorldConstants.PerlinSeedWord
	newInitCircuit.SpaceTypeKey = game.WorldConstants.MiMCSeedWord

	ccs, _ := frontend.Compile(ecc.BN254.ScalarField(), r1cs.NewBuilder, &newInitCircuit)
	//assert.NoError(t, err, "error compiling init circuit in SetupKeysAndParams")

	// Perform trusted setup
	pk, vk, _ := groth16.Setup(ccs)
	//assert.NoError(t, err, "error performing trusted setup for init circuit in SetupKeysAndParams")

	keys.InitVerifyingKey = vk
	InitProvingKey = pk
	InitCCS = ccs

	var newMoveCircuit move.MoveCircuit
	newMoveCircuit.PlanetHashKey = game.WorldConstants.PerlinSeedWord
	newMoveCircuit.SpaceTypeKey = game.WorldConstants.MiMCSeedWord

	ccs, _ = frontend.Compile(ecc.BN254.ScalarField(), r1cs.NewBuilder, &newMoveCircuit)
	//assert.NoError(t, err, "error compiling move circuit in SetupKeysAndParams")

	// Perform trusted setup
	pk, vk, _ = groth16.Setup(ccs)
	//assert.NoError(t, err, "error performing trusted setup for move circuit in SetupKeysAndParams")

	keys.MoveVerifyingKey = vk
	MoveProvingKey = pk
	MoveCCS = ccs

	game.NebulaSpaceConstants = game.SpaceConstant{
		Label:                   "Nebula",
		PlanetSpawnThreshold:    "0.005",
		PlanetLevelThreshold:    [11]string{"0.5", "0.8", "0.95", "1", "-1", "-1", "-1", "-1", "-1", "-1", "-1"},
		StatBuffMultiplier:      "1",
		DefenseDebuffMultiplier: "1",
		ScoreMultiplier:         "1",
	}

	game.SafeSpaceConstants = game.SpaceConstant{
		Label:                   "SafeSpace",
		PlanetSpawnThreshold:    "0.01",
		PlanetLevelThreshold:    [11]string{"-1", "0.2", "0.6", "0.85", "0.95", "1", "-1", "-1", "-1", "-1", "-1"},
		StatBuffMultiplier:      "1.25",
		DefenseDebuffMultiplier: "0.5",
		ScoreMultiplier:         "2",
	}

	game.DeepSpaceConstants = game.SpaceConstant{
		Label:                   "DeepSpace",
		PlanetSpawnThreshold:    "0.015",
		PlanetLevelThreshold:    [11]string{"-1", "-1", "-1", "0.1", "0.35", "0.75", "1", "-1", "-1", "-1", "-1"},
		StatBuffMultiplier:      "1.5",
		DefenseDebuffMultiplier: "0.25",
		ScoreMultiplier:         "3",
	}

	game.PlanetLevel0Stats = game.PlanetLevelStats{
		Level:         0,
		EnergyDefault: "0",
		EnergyMax:     "100",
		EnergyRefill:  "60",
		Range:         "20",
		Speed:         "0.5",
		Defense:       "500",
		Score:         "10",
	}

	game.PlanetLevel1Stats = game.PlanetLevelStats{
		Level:         1,
		EnergyDefault: "4",
		EnergyMax:     "600",
		EnergyRefill:  "300",
		Range:         "25",
		Speed:         "0.51",
		Defense:       "300",
		Score:         "30",
	}

	game.PlanetLevel2Stats = game.PlanetLevelStats{
		Level:         2,
		EnergyDefault: "32",
		EnergyMax:     "2300",
		EnergyRefill:  "900",
		Range:         "30",
		Speed:         "0.52",
		Defense:       "300",
		Score:         "80",
	}

	game.PlanetLevel3Stats = game.PlanetLevelStats{
		Level:         3,
		EnergyDefault: "180",
		EnergyMax:     "6000",
		EnergyRefill:  "1800",
		Range:         "35",
		Speed:         "0.53",
		Defense:       "300",
		Score:         "150",
	}

	game.PlanetLevel4Stats = game.PlanetLevelStats{
		Level:         4,
		EnergyDefault: "1000",
		EnergyMax:     "25000",
		EnergyRefill:  "3600",
		Range:         "40",
		Speed:         "0.54",
		Defense:       "250",
		Score:         "250",
	}

	game.PlanetLevel5Stats = game.PlanetLevelStats{
		Level:         5,
		EnergyDefault: "5000",
		EnergyMax:     "100000",
		EnergyRefill:  "10800",
		Range:         "45",
		Speed:         "0.55",
		Defense:       "250",
		Score:         "400",
	}

	game.PlanetLevel6Stats = game.PlanetLevelStats{
		Level:         6,
		EnergyDefault: "20000",
		EnergyMax:     "300000",
		EnergyRefill:  "21600",
		Range:         "50",
		Speed:         "0.56",
		Defense:       "200",
		Score:         "600",
	}

	game.PlanetLevel7Stats = game.PlanetLevelStats{
		Level:         7,
		EnergyDefault: "35000",
		EnergyMax:     "500000",
		EnergyRefill:  "21600",
		Range:         "55",
		Speed:         "0.57",
		Defense:       "200",
		Score:         "0",
	}

	game.PlanetLevel8Stats = game.PlanetLevelStats{
		Level:         8,
		EnergyDefault: "56000",
		EnergyMax:     "700000",
		EnergyRefill:  "28800",
		Range:         "60",
		Speed:         "0.58",
		Defense:       "150",
		Score:         "0",
	}

	game.PlanetLevel9Stats = game.PlanetLevelStats{
		Level:         9,
		EnergyDefault: "72000",
		EnergyMax:     "800000",
		EnergyRefill:  "36000",
		Range:         "65",
		Speed:         "0.59",
		Defense:       "150",
		Score:         "0",
	}

	game.PlanetLevel10Stats = game.PlanetLevelStats{
		Level:         10,
		EnergyDefault: "100000",
		EnergyMax:     "1000000",
		EnergyRefill:  "43200",
		Range:         "70",
		Speed:         "0.6",
		Defense:       "100",
		Score:         "0",
	}

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
}

func QueuePersonaTx(world *cardinal.World, personaTag string, signerAddress string) error {
	// Transaction for creating a Persona with a specific tag and signer to it
	transaction := ecs.CreatePersona{
		PersonaTag:    personaTag,
		SignerAddress: signerAddress,
	}

	// Add tx to queue
	world.TestingAddCreatePersonaTxToQueue(transaction)

	return nil
}

func CreatePlayerWithClaimedPlanet(world *cardinal.World, personaTag string, signerAddress string, locationHash string, perlin int64) (cardinal.EntityID, component.PlayerComponent, error) {
	wCtx := cardinal.TestingWorldToWorldContext(world)

	err := QueuePersonaTx(world, personaTag, signerAddress)
	if err != nil {
		return 0, component.PlayerComponent{}, err
	}

	id, err := cardinal.Create(wCtx, component.PlanetComponent{})
	if err != nil {
		return 0, component.PlayerComponent{}, err
	}

	planetStats, err := utils.GetPlanetStatsByLocationHash(locationHash, perlin)
	if err != nil {
		return 0, component.PlayerComponent{}, err
	}
	planetComponent := component.PlanetComponent{
		Level:               planetStats.Level,
		LocationHash:        locationHash,
		OwnerPersonaTag:     personaTag,
		EnergyCurrent:       utils.StrToDec("1"),
		EnergyMax:           utils.StrToDec(planetStats.EnergyMax),
		Defense:             utils.StrToDec(planetStats.Defense),
		Range:               utils.StrToDec(planetStats.Range),
		Speed:               utils.StrToDec(planetStats.Speed),
		EnergyRefill:        utils.StrToDec(planetStats.EnergyRefill),
		LastUpdateRefillAge: utils.IntToDec(0),
		LastUpdateTick:      utils.IntToDec(int(world.CurrentTick())),
		SpaceArea:           utils.SpaceAreaToInt(utils.GetSpaceArea(perlin)),
	}

	err = planetComponent.Set(wCtx, id)
	if err != nil {
		return 0, component.PlayerComponent{}, err
	}

	id, err = cardinal.Create(wCtx, component.PlayerComponent{})
	if err != nil {
		return 0, component.PlayerComponent{}, err
	}

	playaComponent := component.PlayerComponent{
		PersonaTag:            personaTag,
		HaveClaimedHomePlanet: true,
	}

	err = playaComponent.Set(wCtx, id)

	return id, playaComponent, nil
}

func CreatePlanetByLocationHash(world *cardinal.World, locationHash string, perlin int64, ownerPersona string) (cardinal.EntityID, component.PlanetComponent, error) {
	wCtx := cardinal.TestingWorldToWorldContext(world)
	planetStats, err := utils.GetPlanetStatsByLocationHash(locationHash, perlin)
	if err != nil {
		return 0, component.PlanetComponent{}, err
	}

	id, err := cardinal.Create(wCtx, component.PlanetComponent{})
	if err != nil {
		return 0, component.PlanetComponent{}, err
	}

	var lastUpdateRefillAge *decimal.Big
	if planetStats.EnergyDefault != "0" {
		lastUpdateRefillAge = utils.InvEnergyCurve(utils.InvEnergyCurve(utils.Saturate(new(decimal.Big).Quo(utils.StrToDec(planetStats.EnergyDefault), utils.StrToDec(planetStats.EnergyMax)))))
	} else {
		lastUpdateRefillAge = utils.StrToDec("0")
	}
	newPlanet := component.PlanetComponent{
		Level:               planetStats.Level,
		LocationHash:        locationHash,
		OwnerPersonaTag:     ownerPersona,
		EnergyCurrent:       utils.StrToDec(planetStats.EnergyDefault),
		EnergyMax:           utils.StrToDec(planetStats.EnergyMax),
		EnergyRefill:        utils.StrToDec(planetStats.EnergyRefill),
		Defense:             utils.StrToDec(planetStats.Defense),
		Range:               utils.StrToDec(planetStats.Range),
		Speed:               utils.StrToDec(planetStats.Speed),
		LastUpdateRefillAge: lastUpdateRefillAge,
		LastUpdateTick:      utils.StrToDec(strconv.FormatUint(world.CurrentTick(), 10)),
		SpaceArea:           utils.SpaceAreaToInt(utils.GetSpaceArea(perlin)),
	}

	err = newPlanet.Set(wCtx, id)
	if err != nil {
		return 0, component.PlanetComponent{}, err
	}
	return id, newPlanet, nil
}

func CreateMaxEnergyPlanetByLocationHash(world *cardinal.World, locationHash string, perlin int64, ownerPersona string) (cardinal.EntityID, component.PlanetComponent, error) {
	wCtx := cardinal.TestingWorldToWorldContext(world)
	planetStats, err := utils.GetPlanetStatsByLocationHash(locationHash, perlin)
	if err != nil {
		return 0, component.PlanetComponent{}, err
	}

	id, err := cardinal.Create(wCtx, component.PlanetComponent{})
	if err != nil {
		return 0, component.PlanetComponent{}, err
	}

	var lastUpdateRefillAge *decimal.Big
	if planetStats.EnergyDefault != "0" {
		lastUpdateRefillAge = utils.InvEnergyCurve(utils.InvEnergyCurve(utils.Saturate(new(decimal.Big).Quo(utils.StrToDec(planetStats.EnergyMax), utils.StrToDec(planetStats.EnergyMax)))))
	} else {
		lastUpdateRefillAge = utils.StrToDec("0")
	}
	newPlanet := component.PlanetComponent{
		Level:               planetStats.Level,
		LocationHash:        locationHash,
		OwnerPersonaTag:     ownerPersona,
		EnergyCurrent:       utils.StrToDec(planetStats.EnergyMax),
		EnergyMax:           utils.StrToDec(planetStats.EnergyMax),
		EnergyRefill:        utils.StrToDec(planetStats.EnergyRefill),
		Defense:             utils.StrToDec(planetStats.Defense),
		Range:               utils.StrToDec(planetStats.Range),
		Speed:               utils.StrToDec(planetStats.Speed),
		LastUpdateRefillAge: lastUpdateRefillAge,
		LastUpdateTick:      utils.StrToDec(strconv.FormatUint(world.CurrentTick(), 10)),
		SpaceArea:           utils.SpaceAreaToInt(utils.GetSpaceArea(perlin)),
	}

	err = newPlanet.Set(wCtx, id)
	if err != nil {
		log.Debug().Msgf("Error creating new planet: %+v ", newPlanet)
	}
	return id, newPlanet, nil
}

func GetPlanetEnergiesAfterSendingEnergy(world *cardinal.World, transaction tx.SendEnergyMsg, fromPlanetStartingEnergy *decimal.Big, senderPersona string, energyArrivalTick int64) (senderEnergy *decimal.Big, recipientEnergy *decimal.Big, err error) {
	// Get/create the two planets
	planetFromEntity, ok := component.LoadPlanetComponent(transaction.LocationHashFrom)
	if !ok {
		log.Debug().Msg("sender planet does not exist in the planets index")
		return nil, nil, errors.New("sender planet does not exist in the planets index")
	}
	planetFrom := planetFromEntity.Component
	if planetFrom == (component.PlanetComponent{}) {
		log.Debug().Msg("sender planet does not exist in the planets index")
	}
	planetFrom.EnergyCurrent = fromPlanetStartingEnergy

	planetToStats := &component.PlanetComponent{}
	planetToEntity, ok := component.LoadPlanetComponent(transaction.LocationHashTo)
	if !ok {
		log.Debug().Msgf("planet with location hash %s not found", transaction.LocationHashTo)
	}
	planetTo := planetToEntity.Component
	if planetTo == (component.PlanetComponent{}) {
		stats, err := utils.GetPlanetStatsByLocationHash(transaction.LocationHashTo, transaction.PerlinTo)
		if err != nil {
			return nil, nil, err
		}
		planetToStats.OwnerPersonaTag = ""
		planetToStats.EnergyCurrent = utils.StrToDec(stats.EnergyDefault)
		planetToStats.EnergyRefill = utils.StrToDec(stats.EnergyRefill)
		planetToStats.EnergyMax = utils.StrToDec(stats.EnergyMax)
		planetToStats.Defense = utils.StrToDec(stats.Defense)
		planetToStats.Range = utils.StrToDec(stats.Range)
		planetToStats.Speed = utils.StrToDec(stats.Speed)
		lastUpdateRefillAge := new(decimal.Big).Quo(utils.StrToDec(stats.EnergyDefault), planetToStats.EnergyMax)
		planetToStats.LastUpdateRefillAge = lastUpdateRefillAge
		planetToStats.LastUpdateTick = utils.Int64ToDec(energyArrivalTick)
		planetToStats.SpaceArea = utils.SpaceAreaToInt(utils.GetSpaceArea(transaction.PerlinTo))
	} else {
		planetToStats = &planetTo
		if planetTo.OwnerPersonaTag != "" {
			RefillEnergyWithAsIs(&planetTo, energyArrivalTick)
		}
	}

	// (0) Apply lazy refill for sending planet, this is applied in SendEnergySystem
	RefillEnergyWithAsIs(&planetFrom, int64(world.CurrentTick()))

	// (1) Cut travel cost from energy sent
	postTravelCostEnergy := utils.EnergyOnEmbark(utils.Int64ToDec(transaction.Energy), planetFrom.EnergyMax, utils.Int64ToDec(transaction.MaxDistance), planetFrom.Range)
	log.Debug().Msgf("EnergyOnEmbark: %s", utils.DecToStr(postTravelCostEnergy))

	// (2) Calculate energy remaining after debuff
	var shipEnergyOnArrival *decimal.Big
	if planetToStats.OwnerPersonaTag == senderPersona {
		shipEnergyOnArrival = utils.EnergyOnArrivalAtFriendlyPlanet(postTravelCostEnergy)
	} else {
		shipEnergyOnArrival = utils.EnergyAfterDefenseDebuff(postTravelCostEnergy, planetToStats.Defense)
	}

	log.Debug().Msgf("shipEnergyOnArrival: %s", utils.DecToStr(postTravelCostEnergy))

	// (3) Subtract energy from sender, calculate energy for recipient
	planetFrom.EnergyCurrent = new(decimal.Big).Sub(planetFrom.EnergyCurrent, utils.Int64ToDec(transaction.Energy))
	if utils.LessThan(decimal.New(0, 0), shipEnergyOnArrival) {
		if planetToStats.OwnerPersonaTag == senderPersona {
			// Handle the case where the planet is owned by the player
			e := new(decimal.Big).Add(shipEnergyOnArrival, planetToStats.EnergyCurrent)
			// Clamp the energy to the planet max energy
			planetToStats.EnergyCurrent = utils.DecMin(e, planetToStats.EnergyMax)
		} else {
			// Handle the case where the planet is owned by another player
			postAttackEnergy := new(decimal.Big).Sub(planetToStats.EnergyCurrent, shipEnergyOnArrival)
			isConquered := utils.LessThan(postAttackEnergy, utils.IntToDec(0))
			if isConquered {
				planetToStats.OwnerPersonaTag = senderPersona

				// Reverse the application of the planet's defense before applying the remaining energy to the planet
				reverseDefensePostAttackEnergy := new(decimal.Big).Quo(new(decimal.Big).Mul(postAttackEnergy, planetToStats.Defense), utils.StrToDec("100"))
				// Also, clamp the energy to the planet max energy
				planetToStats.EnergyCurrent = utils.DecMin(new(decimal.Big).Abs(reverseDefensePostAttackEnergy), planetToStats.EnergyMax)
			} else {
				// Handle the case where the planet is not conquered
				planetToStats.EnergyCurrent = postAttackEnergy
			}
		}
		planetToStats.LastUpdateRefillAge = utils.InvEnergyCurve(utils.InvEnergyCurve(utils.Saturate(new(decimal.Big).Quo(planetToStats.EnergyCurrent, planetToStats.EnergyMax))))
		planetToStats.LastUpdateTick = utils.Int64ToDec(energyArrivalTick)
	}

	return planetFrom.EnergyCurrent, planetToStats.EnergyCurrent, nil
}

// RefillEnergyWithRecalc Simulate an energy refill but use InvEnergyCurve()
// to determine what the recalculated normalized refill age would be.
func RefillEnergyWithRecalc(planetToRefill *component.PlanetComponent, currentTick int64) component.PlanetComponent {
	normalizedRefillAge := utils.InvEnergyCurve(utils.InvEnergyCurve(utils.Saturate(new(decimal.Big).Quo(planetToRefill.EnergyCurrent, planetToRefill.EnergyMax))))
	planetToRefill.EnergyCurrent = utils.EnergyLevel(planetToRefill.EnergyMax, normalizedRefillAge)
	planetToRefill.LastUpdateTick = utils.Int64ToDec(currentTick)
	planetToRefill.LastUpdateRefillAge = normalizedRefillAge

	return *planetToRefill
}

// RefillEnergyWithAsIs Simulate an energy refill using the precious lastUpdateRefillAge
// rather than recalculating the age using InvEnergyCurve()
func RefillEnergyWithAsIs(planetToRefill *component.PlanetComponent, currentTick int64) component.PlanetComponent {
	log.Debug().Msgf("Applying lazy energy refill to planet: %s", planetToRefill.LocationHash)
	normalizedRefillAge := utils.NormalizedRefillAge(planetToRefill.LastUpdateRefillAge, planetToRefill.LastUpdateTick, utils.Int64ToDec(currentTick), utils.ScaleUpByTickRate(planetToRefill.EnergyRefill))
	planetToRefill.EnergyCurrent = utils.EnergyLevel(planetToRefill.EnergyMax, normalizedRefillAge)
	planetToRefill.LastUpdateTick = utils.Int64ToDec(currentTick)
	planetToRefill.LastUpdateRefillAge = normalizedRefillAge
	log.Debug().Msgf("Updated energy of planet with location hash %s to %s", planetToRefill.LocationHash, utils.DecToStr(planetToRefill.EnergyCurrent))

	return *planetToRefill
}

func GetPlanetByLocationHash(wCtx cardinal.WorldContext, locationHash string) (planet *component.PlanetComponent, err error) {
	search, err := wCtx.NewSearch(cardinal.Exact(component.PlanetComponent{}))
	if err != nil {
		return nil, err
	}
	err = search.Each(wCtx, func(id cardinal.EntityID) bool {
		pc, err := cardinal.GetComponent[component.PlanetComponent](wCtx, id)
		if err != nil {
			return false
		}
		if pc.LocationHash == locationHash {
			planet = pc
		}
		return true
	})
	if err != nil {
		return nil, err
	}
	return planet, err
}
