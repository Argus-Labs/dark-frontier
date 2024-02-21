package utils

import (
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"github.com/argus-labs/darkfrontier-backend/cardinal/tx"
	"github.com/argus-labs/darkfrontier-backend/circuit/initialize"
	"github.com/stretchr/testify/assert"
	"math/big"
	"pkg.world.dev/world-engine/cardinal"
	"pkg.world.dev/world-engine/sign"
	"strconv"
	"testing"
)

func SendEnergy(world *cardinal.World, transaction tx.SendEnergyMsg, persona string) {
	signedPayload := sign.Transaction{
		PersonaTag: persona,
	}
	tx.SendEnergy.AddToQueue(world, transaction, &signedPayload)
}

func SetConstant(world *cardinal.World, transaction tx.SetConstantMsg, persona string) {
	signedPayload := sign.Transaction{
		PersonaTag: persona,
	}
	tx.SetConstant.AddToQueue(world, transaction, &signedPayload)
}

func ClaimHomePlanet(t *testing.T, planet NewPlanetInfo, persona string) (cardinal.World, cardinal.WorldContext, func()) {
	// 0) Setup world
	world, doTick := ScaffoldTestWorld(t)
	wCtx := cardinal.TestingWorldToWorldContext(world)

	// 1) Claim a planet as "Player1"
	err := QueuePersonaTx(world, persona, "0x1")
	assert.NoError(t, err)

	// 2) Try to claim a home planet with "Player1"
	// 2a) Generate proof for the transaction
	pub, ok := new(big.Int).SetString(planet.LocationHash, 16)
	assert.True(t, ok)

	initAssignment := initialize.InitCircuit{
		X:       planet.X,
		Y:       planet.Y,
		R:       strconv.FormatInt(game.WorldConstants.RadiusMax, 10),
		Scale:   strconv.Itoa(game.WorldConstants.Scale),
		XMirror: strconv.Itoa(game.WorldConstants.XMirror),
		YMirror: strconv.Itoa(game.WorldConstants.YMirror),
		Pub:     pub,
		Perl:    strconv.FormatInt(planet.Perlin, 10),
	}
	proof, err := getProofForInitCircuit(t, initAssignment)
	assert.NoError(t, err)

	// 2b) Send transaction
	transaction := tx.ClaimHomePlanetMsg{
		LocationHash: planet.LocationHash,
		Perlin:       planet.Perlin,
		Proof:        proof,
	}

	signedPayload := sign.Transaction{
		PersonaTag: "Player1",
	}
	tx.ClaimHomePlanet.AddToQueue(world, transaction, &signedPayload)

	// 2c) Run the world so the claim is attempted
	doTick()

	return *world, wCtx, doTick
}
