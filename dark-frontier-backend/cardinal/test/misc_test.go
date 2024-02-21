package utils

import (
	"github.com/argus-labs/darkfrontier-backend/cardinal/component"
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"github.com/argus-labs/darkfrontier-backend/cardinal/system"
	"github.com/argus-labs/darkfrontier-backend/cardinal/tx"
	"github.com/argus-labs/darkfrontier-backend/cardinal/utils"
	"github.com/argus-labs/darkfrontier-backend/circuit/initialize"
	"github.com/rs/zerolog/log"
	"github.com/stretchr/testify/assert"
	"math/big"
	"os"
	"pkg.world.dev/world-engine/cardinal"
	"pkg.world.dev/world-engine/sign"
	"strconv"
	"sync"
	"testing"
)

func TestCannotClaimPlanetAfterTimerIsPast(t *testing.T) {
	// 0) Setup world
	world, doTick := ScaffoldTestWorld(t)
	err := os.Setenv("TIMER", "1")
	if err != nil {
		log.Debug().Msgf("Failed to set env var TIMER: %s", err)
	}
	err = utils.SetConstantsFromEnv()
	if err != nil {
		log.Debug().Msgf("Failed to set instance timer: %s", err)
	}
	player1 := "Player1"

	// 1) Claim a planet as "Player1"
	err = QueuePersonaTx(world, player1, "0x1")
	assert.NoError(t, err)

	// 1a) Run 2 tick so current tick (2) will be greater than timer (1) afterwards
	// 1a) Run 5 tick so current tick (5) will be greater than timer (where 1 second = 5 ticks)
	for i := 0; i < 5; i++ {
		doTick()
	}
	assert.Equal(t, uint64(5), world.CurrentTick())

	// 2) Try to claim a home planet with "Player1" who already has a home planet
	// 2a) Generate proof for the transaction
	pub, ok := new(big.Int).SetString(levelZeroPlanet.LocationHash, 16)
	assert.True(t, ok)

	initAssignment := initialize.InitCircuit{
		X:       levelZeroPlanet.X,
		Y:       levelZeroPlanet.Y,
		R:       strconv.FormatInt(game.WorldConstants.RadiusMax, 10),
		Scale:   strconv.Itoa(game.WorldConstants.Scale),
		XMirror: strconv.Itoa(game.WorldConstants.XMirror),
		YMirror: strconv.Itoa(game.WorldConstants.YMirror),
		Pub:     pub,
		Perl:    strconv.FormatInt(levelZeroPlanet.Perlin, 10),
	}
	proof, err := getProofForInitCircuit(t, initAssignment)
	assert.NoError(t, err)

	// 2b) Send transaction
	transaction := tx.ClaimHomePlanetMsg{
		LocationHash: levelZeroPlanet.LocationHash,
		Perlin:       levelZeroPlanet.Perlin,
		Proof:        proof,
	}

	signedPayload := sign.Transaction{
		PersonaTag: "Player1",
	}
	tx.ClaimHomePlanet.AddToQueue(world, transaction, &signedPayload)

	// 2c) Run the world so the claim is attempted
	doTick()

	// 3) Test that a planet was NOT created at this location hash
	planetEntity, _ := component.LoadPlanetComponent(levelZeroPlanet.LocationHash)
	assert.Equal(t, component.PlanetEntity{}, planetEntity)

	err = world.ShutDown()
	assert.NoError(t, err)
}

func TestIndexesRebuildAfterShutdown(t *testing.T) {
	// 1. Claim a planet so that the planet and player indexes get populated
	// 0) Setup world
	world, doTick := ScaffoldTestWorld(t)
	wCtx := cardinal.TestingWorldToWorldContext(world)
	player1 := "Player1"

	// 1) Claim a planet as "Player1"
	err := QueuePersonaTx(world, player1, "0x1")
	assert.NoError(t, err)

	// 2) Try to claim a home planet with "Player1"
	// 2a) Generate proof for the transaction
	pub, ok := new(big.Int).SetString(levelZeroPlanet.LocationHash, 16)
	assert.True(t, ok)

	initAssignment := initialize.InitCircuit{
		X:       levelZeroPlanet.X,
		Y:       levelZeroPlanet.Y,
		R:       strconv.FormatInt(game.WorldConstants.RadiusMax, 10),
		Scale:   strconv.Itoa(game.WorldConstants.Scale),
		XMirror: strconv.Itoa(game.WorldConstants.XMirror),
		YMirror: strconv.Itoa(game.WorldConstants.YMirror),
		Pub:     pub,
		Perl:    strconv.FormatInt(levelZeroPlanet.Perlin, 10),
	}
	proof, err := getProofForInitCircuit(t, initAssignment)
	assert.NoError(t, err)

	// 2b) Send transaction
	transaction := tx.ClaimHomePlanetMsg{
		LocationHash: levelZeroPlanet.LocationHash,
		Perlin:       levelZeroPlanet.Perlin,
		Proof:        proof,
	}

	signedPayload := sign.Transaction{
		PersonaTag: "Player1",
	}
	tx.ClaimHomePlanet.AddToQueue(world, transaction, &signedPayload)

	// 2c) Run the world so the claim is attempted
	doTick()

	// 3) Test if a planet was created with the given location hash
	planetEntity, ok := component.LoadPlanetComponent(levelZeroPlanet.LocationHash)
	assert.True(t, ok)
	assert.NotEmpty(t, planetEntity)

	// 4) Check whether the player's personaTag is now the owner of a Planet
	planetComp, err := cardinal.GetComponent[component.PlanetComponent](wCtx, planetEntity.EntityId)
	assert.Equal(t, player1, planetComp.OwnerPersonaTag)

	// 2. Simulate a shutdown
	system.RebuildIndex = true
	component.PlanetIndex = sync.Map{}
	component.ShipIndex = sync.Map{}
	component.PlayerIndex = sync.Map{}
	planetEntity, ok = component.LoadPlanetComponent(levelZeroPlanet.LocationHash)
	assert.False(t, ok) // Assert that the planet doesn't exist in index anymore

	// 3. Do a tick so that indexes get rebuilt
	doTick()

	// 4. Assert that the planet is back in the index
	planetEntity, ok = component.LoadPlanetComponent(levelZeroPlanet.LocationHash)
	assert.True(t, ok)
	assert.NotEmpty(t, planetEntity)

	err = world.ShutDown()
	assert.NoError(t, err)
}

func TestRebalancingPlanetLevel(t *testing.T) {
	// 0) Force build DefaultsComponent
	system.RebuildIndex = true

	// 1) Claim a home planet for "Player1"
	world, wCtx, doTick := ClaimHomePlanet(t, levelZeroPlanet, "Player1")
	temp := game.PlanetLevel0Stats

	// 2) Create SetConstant transaction
	transaction := tx.SetConstantMsg{
		ConstantName: "Level0Constants",
		Value: system.LevelConstantsMsg{
			EnergyDefault: game.PlanetLevel0Stats.EnergyDefault,
			EnergyMax:     "5000",
			EnergyRefill:  game.PlanetLevel0Stats.EnergyRefill,
			Range:         game.PlanetLevel0Stats.Range,
			Speed:         game.PlanetLevel0Stats.Speed,
			Defense:       "250",
			Score:         game.PlanetLevel0Stats.Score,
		},
	}
	signedPayload := sign.Transaction{
		PersonaTag: "admin",
	}
	tx.SetConstant.AddToQueue(&world, transaction, &signedPayload)

	// 3) Do a tick so that the transaction is processed
	doTick()

	// 4) Check that existing planet was changed
	planet, err := GetPlanetByLocationHash(wCtx, levelZeroPlanet.LocationHash)
	assert.NoError(t, err)
	assert.Equal(t, utils.StrToDec("5000"), planet.EnergyMax)
	assert.Equal(t, utils.StrToDec("250"), planet.Defense)

	// 5) Check that defaults component was updated correctly
	dc, _, err := component.GetDefaultsComponent(wCtx)
	assert.NoError(t, err)
	assert.Equal(t, "5000", dc.Level0PlanetStats.EnergyMax)
	assert.Equal(t, "250", dc.Level0PlanetStats.Defense)

	// 6) Check that game constants was changed
	assert.Equal(t, "5000", game.PlanetLevel0Stats.EnergyMax)
	assert.Equal(t, "250", game.PlanetLevel0Stats.Defense)
	assert.Equal(t, &game.PlanetLevel0Stats, game.BasePlanetLevelStats[0])

	*game.BasePlanetLevelStats[0] = temp
	err = world.ShutDown()
	assert.NoError(t, err)
}

func TestRebalancingSpaceArea(t *testing.T) {
	// 0) Force build DefaultsComponent
	system.RebuildIndex = true

	// 1) Claim a home planet for "Player1"
	world, wCtx, doTick := ClaimHomePlanet(t, levelZeroPlanet, "Player1")
	temp := game.SafeSpaceConstants

	// 2) Create SetConstant transaction
	transaction := tx.SetConstantMsg{
		ConstantName: "SafeSpaceConstants",
		Value: system.SpaceConstantsMsg{
			StatBuffMultiplier:      "5",
			DefenseDebuffMultiplier: "5",
			ScoreMultiplier:         "5",
		},
	}
	signedPayload := sign.Transaction{
		PersonaTag: "admin",
	}
	tx.SetConstant.AddToQueue(&world, transaction, &signedPayload)

	// 3) Do a tick so that the transaction is processed
	doTick()

	// 4) Check that existing planet was changed
	planet, err := GetPlanetByLocationHash(wCtx, levelZeroPlanet.LocationHash)
	assert.NoError(t, err)
	// Both energy max and defense would be multiplied by 5x
	assert.Equal(t, utils.StrToDec("500"), planet.EnergyMax)
	assert.Equal(t, utils.StrToDec("2500"), planet.Defense)

	// 5) Check that defaults component was updated correctly
	dc, _, err := component.GetDefaultsComponent(wCtx)
	assert.NoError(t, err)
	assert.Equal(t, "5", dc.SafeSpaceConstants.StatBuffMultiplier)
	assert.Equal(t, "5", dc.SafeSpaceConstants.DefenseDebuffMultiplier)
	assert.Equal(t, "5", dc.SafeSpaceConstants.ScoreMultiplier)

	// 6) Check that game constants was changed
	assert.Equal(t, "5", game.SafeSpaceConstants.StatBuffMultiplier)
	assert.Equal(t, "5", game.SafeSpaceConstants.DefenseDebuffMultiplier)
	assert.Equal(t, "5", game.SafeSpaceConstants.ScoreMultiplier)
	assert.Equal(t, &game.SafeSpaceConstants, game.SpaceConstants[1])

	*game.SpaceConstants[1] = temp
	err = world.ShutDown()
	assert.NoError(t, err)
}
