package utils

import (
	"github.com/argus-labs/darkfrontier-backend/cardinal/component"
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"github.com/argus-labs/darkfrontier-backend/cardinal/query"
	"github.com/argus-labs/darkfrontier-backend/cardinal/tx"
	"github.com/argus-labs/darkfrontier-backend/circuit/initialize"
	"github.com/argus-labs/darkfrontier-backend/circuit/move"
	"github.com/stretchr/testify/assert"
	"math/big"
	"pkg.world.dev/world-engine/cardinal"
	"pkg.world.dev/world-engine/sign"
	"strconv"
	"testing"
)

func TestReadPlanetsClaimingHomePlanet(t *testing.T) {
	// 0) Setup world
	world, doTick := ScaffoldTestWorld(t)
	wCtx := cardinal.TestingWorldToWorldContext(world)
	player1 := "Player1"

	// 1) Claim a planet as "Player1"
	err := QueuePersonaTx(world, player1, "0x1")
	assert.NoError(t, err)

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

	// 3) Test if a planet was created with the given location hash
	planetEntity, ok := component.LoadPlanetComponent(levelZeroPlanet.LocationHash)
	assert.Equal(t, true, ok)
	assert.NotEqual(t, cardinal.EntityID(0x0), planetEntity.EntityId)

	// 4) Check whether the player's personaTag is now the owner of a Planet
	assert.Equal(t, player1, planetEntity.Component.OwnerPersonaTag)

	// Send query-current-state request
	planetList := []string{levelZeroPlanet.LocationHash}
	req := query.PlanetsMsg{
		PlanetsList: planetList,
	}

	// Assert that 2 planets are query and are owned by the correct planets
	reply, err := query.Planets(wCtx, &req)
	assert.Equal(t, 1, len(reply.Planets))
	assert.Equal(t, player1, reply.Planets[0].OwnerPersonaTag)

	err = world.ShutDown()
	assert.NoError(t, err)
}

func TestReadPlanetsWithEnergyTransfer(t *testing.T) {
	// 0) Setup world
	world, doTick := ScaffoldTestWorld(t)
	wCtx := cardinal.TestingWorldToWorldContext(world)
	player1 := "Player1"

	// 1) Create two planets to send from, make them both owned by Player1
	_, fromPlanet, err := CreateMaxEnergyPlanetByLocationHash(world, levelTwoPlanet.LocationHash, levelTwoPlanet.Perlin, player1)
	assert.NoError(t, err)
	_, toPlanet, err := CreateMaxEnergyPlanetByLocationHash(world, levelTwoPlanetTwo.LocationHash, levelTwoPlanetTwo.Perlin, player1)
	assert.NoError(t, err)

	distance := 15 // I don't have the exact value for this, but this dist should be good to make the test pass

	// 3) Send energy from non-owned planet to other planet
	// 3a) Generate proof for the transaction
	pub1, ok1 := new(big.Int).SetString(levelTwoPlanet.LocationHash, 16)
	assert.True(t, ok1)
	pub2, ok2 := new(big.Int).SetString(levelTwoPlanetTwo.LocationHash, 16)
	assert.True(t, ok2)
	moveAssignment := move.MoveCircuit{
		X1:      levelTwoPlanet.X,
		Y1:      levelTwoPlanet.Y,
		X2:      levelTwoPlanetTwo.X,
		Y2:      levelTwoPlanetTwo.Y,
		R:       strconv.FormatInt(game.WorldConstants.RadiusMax, 10),
		DistMax: strconv.Itoa(distance),
		Scale:   strconv.Itoa(game.WorldConstants.Scale),
		XMirror: strconv.Itoa(game.WorldConstants.XMirror),
		YMirror: strconv.Itoa(game.WorldConstants.YMirror),
		Pub1:    pub1,
		Pub2:    pub2,
		Perl2:   strconv.FormatInt(levelTwoPlanetTwo.Perlin, 10),
	}
	proof, err := getProofForMoveCircuit(t, moveAssignment)
	assert.NoError(t, err)

	transaction := tx.SendEnergyMsg{
		LocationHashFrom: fromPlanet.LocationHash,
		LocationHashTo:   toPlanet.LocationHash,
		Energy:           1500,
		PerlinTo:         levelTwoPlanetTwo.Perlin,
		RadiusTo:         game.WorldConstants.RadiusMax,
		MaxDistance:      int64(distance),
		Proof:            proof,
	}

	// 3b) Send the transaction (energy)
	SendEnergy(world, transaction, player1)
	assert.NoError(t, err)

	// Tick once to send energy
	doTick()

	// Send query-current-state request
	planetList := []string{fromPlanet.LocationHash, toPlanet.LocationHash}
	req := query.PlanetsMsg{
		PlanetsList: planetList,
	}
	reply, err := query.Planets(wCtx, &req)

	// Assert that the reply is what we expected
	transfersForSender := reply.Planets[0].EnergyTransfers
	transfersForReceiver := reply.Planets[1].EnergyTransfers
	assert.Equal(t, len(transfersForSender), 1)
	assert.Equal(t, len(transfersForReceiver), 1)
	assert.Equal(t, transfersForSender[0].PlanetFromHash, fromPlanet.LocationHash)
	assert.Equal(t, transfersForReceiver[0].PlanetFromHash, fromPlanet.LocationHash)
	assert.Equal(t, transfersForSender[0].PlanetToHash, toPlanet.LocationHash)
	assert.Equal(t, transfersForReceiver[0].PlanetToHash, toPlanet.LocationHash)

	err = world.ShutDown()
	assert.NoError(t, err)
}

func TestReadAfterSettingConstants(t *testing.T) {
	// 0) Setup world
	world, doTick := ScaffoldTestWorld(t)
	wCtx := cardinal.TestingWorldToWorldContext(world)
	persona := "admin"

	// 1) Claim a planet as "Player1"
	err := QueuePersonaTx(world, persona, "0xd5e099c71b797516c10ed0f0d895f429c2781142")
	assert.NoError(t, err)

	setRadiusTx := tx.SetConstantMsg{
		ConstantName: "Radius",
		Value:        float64(3000), // Send as float to simulate JSON
	}
	setTimerTx := tx.SetConstantMsg{
		ConstantName: "Timer",
		Value:        float64(10),
	}
	setInstanceNameTx := tx.SetConstantMsg{
		ConstantName: "InstanceName",
		Value:        "NewInstanceName",
	}
	SetConstant(world, setRadiusTx, persona)
	SetConstant(world, setTimerTx, persona)
	SetConstant(world, setInstanceNameTx, persona)

	doTick()

	req := query.ConstantMsg{ConstantLabel: "world"}
	reply, err := query.Constants(wCtx, &req)

	constants := reply.Constants.(*game.WorldConstant)
	assert.Equal(t, int64(3000), constants.RadiusMax)
	assert.Equal(t, 10, constants.InstanceTimer)
	assert.Equal(t, "NewInstanceName", constants.InstanceName)
	assert.NoError(t, err)

	err = world.ShutDown()
	assert.NoError(t, err)
}
