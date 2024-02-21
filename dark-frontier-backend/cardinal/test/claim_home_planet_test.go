package utils

import (
	"github.com/argus-labs/darkfrontier-backend/cardinal/component"
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"github.com/argus-labs/darkfrontier-backend/cardinal/tx"
	"github.com/argus-labs/darkfrontier-backend/circuit/initialize"
	"github.com/stretchr/testify/assert"
	"math/big"
	"os"
	"pkg.world.dev/world-engine/cardinal"
	"pkg.world.dev/world-engine/sign"
	"strconv"
	"testing"
)

func TestMain(m *testing.M) {
	// Setup circuits and overwrite constants to use old constants that
	// work with the tests
	SetupCircuitsAndOverwriteConstants()
	// Run the tests
	code := m.Run()
	os.Exit(code)
}

// Verify that the player can successfully claim a valid planet
func TestPlayerCanClaimValidHomePlanet(t *testing.T) {
	// 1) Claim a level zero planet and setup the world
	world, wCtx, _ := ClaimHomePlanet(t, levelZeroPlanet, "Player1")

	// 2) Test if a planet was created with the given location hash
	planetEntity, _ := component.LoadPlanetComponent(levelZeroPlanet.LocationHash)
	assert.NotEqual(t, planetEntity, component.PlanetEntity{})

	// 3) Check whether the player's personaTag is now the owner of a Planet
	planetComp, err := cardinal.GetComponent[component.PlanetComponent](wCtx, planetEntity.EntityId)
	assert.Equal(t, "Player1", planetComp.OwnerPersonaTag)

	err = world.ShutDown()
	assert.NoError(t, err)
}

// Players with claimed planets cannot claim another
func TestPlayerWithPlanetCannotClaim(t *testing.T) {
	// 0) Setup world
	world, doTick := ScaffoldTestWorld(t)

	// 1) Claim a planet as "Player1"
	_, _, err := CreatePlayerWithClaimedPlanet(world, "Player1", "0x1", levelZeroPlanet.LocationHash, levelZeroPlanet.Perlin)
	assert.NoError(t, err)

	// 2) Try to claim a home planet with "Player1" who already has a home planet
	// 2a) Generate proof for the transaction
	pub, ok := new(big.Int).SetString(levelZeroPlanetTwo.LocationHash, 16)
	assert.True(t, ok)
	initAssignment := initialize.InitCircuit{
		X:       levelZeroPlanetTwo.X,
		Y:       levelZeroPlanetTwo.Y,
		R:       strconv.FormatInt(game.WorldConstants.RadiusMax, 10),
		Scale:   strconv.Itoa(game.WorldConstants.Scale),
		XMirror: strconv.Itoa(game.WorldConstants.XMirror),
		YMirror: strconv.Itoa(game.WorldConstants.YMirror),
		Pub:     pub,
		Perl:    strconv.FormatInt(levelZeroPlanetTwo.Perlin, 10),
	}
	proof, err := getProofForInitCircuit(t, initAssignment)
	assert.NoError(t, err)

	// 2b) Send transaction
	transaction := tx.ClaimHomePlanetMsg{
		LocationHash: levelZeroPlanetTwo.LocationHash,
		Perlin:       levelZeroPlanetTwo.Perlin,
		Proof:        proof,
	}

	signedPayload := sign.Transaction{
		PersonaTag: "Player1",
	}
	tx.ClaimHomePlanet.AddToQueue(world, transaction, &signedPayload)

	// 2c) Run the world so the claim is attempted
	doTick()

	// 3) Test if a planet was created with the given location hash
	planetEntity, ok := component.LoadPlanetComponent(levelZeroPlanetTwo.LocationHash)
	assert.Equal(t, false, ok)
	assert.Equal(t, cardinal.EntityID(0x0), planetEntity.EntityId)

	err = world.ShutDown()
	assert.NoError(t, err)
	world = &cardinal.World{}
}

// Claimed planets cannot be claimed again
func TestClaimedPlanetCannotBeClaimed(t *testing.T) {
	// 0) Setup world
	world, doTick := ScaffoldTestWorld(t)
	wCtx := cardinal.TestingWorldToWorldContext(world)
	player2 := "Player2"

	// 1) Claim a planet as "Player1"
	_, _, err := CreatePlayerWithClaimedPlanet(world, "Player1", "0x1", levelZeroPlanet.LocationHash, levelZeroPlanet.Perlin)
	assert.NoError(t, err)

	// 2) Try to claim the same planet as "Player2"
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
		PersonaTag: player2,
	}
	tx.ClaimHomePlanet.AddToQueue(world, transaction, &signedPayload)

	// 2c) Run the world so the claim is attempted
	doTick()

	// 3) Check that Player2 does not exist because if the claim was successful, Player2 will exist
	var playerWasCreatedForPlanetCreation bool

	search, _ := wCtx.NewSearch(cardinal.Exact(component.PlayerComponent{}))
	search.Each(wCtx, func(id cardinal.EntityID) bool {
		obj, err := cardinal.GetComponent[component.PlayerComponent](wCtx, id)
		if err != nil {
			return true
		}
		if obj.PersonaTag == player2 {
			playerWasCreatedForPlanetCreation = true
			return false
		}
		return true
	})
	assert.False(t, playerWasCreatedForPlanetCreation)

	err = world.ShutDown()
	assert.NoError(t, err)
}

// The planet to be claimed must have planetStats.Level = 0
func TestCannotClaimNonLevelZeroPlanet(t *testing.T) {
	// 1) Attempt to claim a planet and setup the world
	world, _, _ := ClaimHomePlanet(t, levelTwoPlanet, "Player1")

	// 2) Test if a planet was created with the given location hash
	planetEntity, ok := component.LoadPlanetComponent(levelTwoPlanet.LocationHash)
	assert.Equal(t, false, ok)
	assert.Equal(t, cardinal.EntityID(0x0), planetEntity.EntityId)

	err := world.ShutDown()
	assert.NoError(t, err)
}

// Player is set as the owner when planet is claimed
func TestPlayerIsOwnerOfClaimedPlanet(t *testing.T) {
	// 1) Set up the world and claim a planet
	world, _, _ := ClaimHomePlanet(t, levelZeroPlanet, "Player1")

	// 2) Assert that the planet now exists and the owner is Player1
	planetEntity, ok := component.LoadPlanetComponent(levelZeroPlanet.LocationHash)
	assert.Equal(t, true, ok)
	assert.Equal(t, "Player1", planetEntity.Component.OwnerPersonaTag)

	err := world.ShutDown()
	assert.NoError(t, err)
}
