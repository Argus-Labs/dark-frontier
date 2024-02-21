package utils

import (
	"github.com/argus-labs/darkfrontier-backend/cardinal/component"
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"github.com/argus-labs/darkfrontier-backend/cardinal/tx"
	"github.com/argus-labs/darkfrontier-backend/cardinal/utils"
	"github.com/argus-labs/darkfrontier-backend/circuit/move"
	"github.com/rotisserie/eris"
	"github.com/stretchr/testify/assert"
	"math/big"
	"pkg.world.dev/world-engine/cardinal"
	"pkg.world.dev/world-engine/sign"
	"strconv"
	"testing"
)

// 2) Player cannot send energy from a planet they don't own
var eps = utils.StrToDec("0.000000001")

func TestCannotSendFromPlanetNotOwned(t *testing.T) {
	// 0) Setup world
	world, doTick := ScaffoldTestWorld(t)

	// 1) Create two planets, one owned and one not
	_, ownedPlanet, err := CreatePlanetByLocationHash(world, levelTwoPlanet.LocationHash, levelTwoPlanet.Perlin, "Player1")
	assert.NoError(t, err)
	_, nonOwnedPlanet, err := CreateMaxEnergyPlanetByLocationHash(world, levelZeroPlanet.LocationHash, levelZeroPlanet.Perlin, "Player2")
	assert.NoError(t, err)

	// 2) Send energy from non-owned planet to other planet
	// 2a) Generate proof for the transaction
	pub1, ok1 := new(big.Int).SetString(levelZeroPlanet.LocationHash, 16)
	assert.True(t, ok1)
	pub2, ok2 := new(big.Int).SetString(levelTwoPlanet.LocationHash, 16)
	assert.True(t, ok2)
	moveAssignment := move.MoveCircuit{
		X1:      levelZeroPlanet.X,
		Y1:      levelZeroPlanet.Y,
		X2:      levelTwoPlanet.X,
		Y2:      levelTwoPlanet.Y,
		R:       strconv.FormatInt(game.WorldConstants.RadiusMax, 10),
		DistMax: "53", // I don't have the exact value for this, but this is plenty to pass
		Scale:   strconv.Itoa(game.WorldConstants.Scale),
		XMirror: strconv.Itoa(game.WorldConstants.XMirror),
		YMirror: strconv.Itoa(game.WorldConstants.YMirror),
		Pub1:    pub1,
		Pub2:    pub2,
		Perl2:   strconv.FormatInt(levelTwoPlanet.Perlin, 10),
	}
	proof, err := getProofForMoveCircuit(t, moveAssignment)
	assert.NoError(t, err)

	// 2b) Send transaction to Cardinal
	transaction := tx.SendEnergyMsg{
		LocationHashFrom: nonOwnedPlanet.LocationHash,
		LocationHashTo:   ownedPlanet.LocationHash,
		Energy:           1,
		PerlinTo:         levelTwoPlanet.Perlin,
		RadiusTo:         game.WorldConstants.RadiusMax,
		MaxDistance:      53,
		Proof:            proof,
	}
	signedPayload := sign.Transaction{
		PersonaTag: "Player1",
	}
	txHash := tx.SendEnergy.AddToQueue(world, transaction, &signedPayload)

	// 2c) Run tick so that the ship can be processed
	sentTick := world.CurrentTick()
	doTick()

	// 3) Assert that the correct error was thrown in the system
	receipts, _ := world.TestingGetTransactionReceiptsForTick(sentTick)
	assert.Equal(t, eris.Errorf("player with persona %s does not own planet with location hash %s", "Player1", nonOwnedPlanet.LocationHash).Error(), receipts[0].Errs[0].Error())
	assert.Equal(t, txHash, receipts[0].TxHash)

	err = world.ShutDown()
	assert.NoError(t, err)
}

// 3) You cannot send more energy than the planet has
func TestCannotSendMoreEnergyThanCurrent(t *testing.T) {
	// 0) Setup world
	world, doTick := ScaffoldTestWorld(t)

	// 1) Create two planets for sending and receiving respectively
	_, fromPlanet, err := CreatePlanetByLocationHash(world, levelZeroPlanet.LocationHash, levelZeroPlanet.Perlin, "Player1")
	assert.NoError(t, err)
	_, toPlanet, err := CreatePlanetByLocationHash(world, levelTwoPlanet.LocationHash, levelTwoPlanet.Perlin, "Player2")
	assert.NoError(t, err)

	// 2) Send energy from non-owned planet to other planet
	// 2a) Generate proof for the transaction
	pub1, ok1 := new(big.Int).SetString(levelZeroPlanet.LocationHash, 16)
	assert.True(t, ok1)
	pub2, ok2 := new(big.Int).SetString(levelTwoPlanet.LocationHash, 16)
	assert.True(t, ok2)
	moveAssignment := move.MoveCircuit{
		X1:      levelZeroPlanet.X,
		Y1:      levelZeroPlanet.Y,
		X2:      levelTwoPlanet.X,
		Y2:      levelTwoPlanet.Y,
		R:       strconv.FormatInt(game.WorldConstants.RadiusMax, 10),
		DistMax: "53", // I don't have the exact value for this, but this is plenty to pass
		Scale:   strconv.Itoa(game.WorldConstants.Scale),
		XMirror: strconv.Itoa(game.WorldConstants.XMirror),
		YMirror: strconv.Itoa(game.WorldConstants.YMirror),
		Pub1:    pub1,
		Pub2:    pub2,
		Perl2:   strconv.FormatInt(levelTwoPlanet.Perlin, 10),
	}
	proof, err := getProofForMoveCircuit(t, moveAssignment)
	assert.NoError(t, err)

	// 2b) Send transaction to Cardinal
	transaction := tx.SendEnergyMsg{
		LocationHashFrom: fromPlanet.LocationHash,
		LocationHashTo:   toPlanet.LocationHash,
		Energy:           1,
		PerlinTo:         levelTwoPlanet.Perlin,
		RadiusTo:         game.WorldConstants.RadiusMax,
		MaxDistance:      53,
		Proof:            proof,
	}
	signedPayload := sign.Transaction{
		PersonaTag: "Player1",
	}
	txHash := tx.SendEnergy.AddToQueue(world, transaction, &signedPayload)

	// 2c) Run tick so that the ship can be processed
	sentTick := world.CurrentTick()
	doTick()

	// 3) Assert that the correct error was thrown in the system
	receipts, _ := world.TestingGetTransactionReceiptsForTick(sentTick)
	assert.Equal(t, eris.Errorf("origin planet with hash %s did not have enough energy", levelZeroPlanet.LocationHash).Error(), receipts[0].Errs[0].Error())
	assert.Equal(t, txHash, receipts[0].TxHash)

	err = world.ShutDown()
	assert.NoError(t, err)
}

// 4) Verify that ships that cannot reach their destination are not sent
func TestSendSmallEnergyToLargeDistancePlanet(t *testing.T) {
	// 0) Setup world
	world, doTick := ScaffoldTestWorld(t)

	// 1) Create two planets for sending and receiving respectively
	_, fromPlanet, err := CreateMaxEnergyPlanetByLocationHash(world, levelTwoPlanet.LocationHash, levelTwoPlanet.Perlin, "Player1")
	assert.NoError(t, err)
	_, toPlanet, err := CreatePlanetByLocationHash(world, levelZeroPlanet.LocationHash, levelZeroPlanet.Perlin, "Player2")
	assert.NoError(t, err)

	// 2) Send energy from non-owned planet to other planet
	// 2a) Generate proof for the transaction
	pub1, ok1 := new(big.Int).SetString(levelTwoPlanet.LocationHash, 16)
	assert.True(t, ok1)
	pub2, ok2 := new(big.Int).SetString(levelZeroPlanet.LocationHash, 16)
	assert.True(t, ok2)
	moveAssignment := move.MoveCircuit{
		X1:      levelTwoPlanet.X,
		Y1:      levelTwoPlanet.Y,
		X2:      levelZeroPlanet.X,
		Y2:      levelZeroPlanet.Y,
		R:       strconv.FormatInt(game.WorldConstants.RadiusMax, 10),
		DistMax: "500", // I don't have the exact value for this, but this dist should be far enough to make the test pass
		Scale:   strconv.Itoa(game.WorldConstants.Scale),
		XMirror: strconv.Itoa(game.WorldConstants.XMirror),
		YMirror: strconv.Itoa(game.WorldConstants.YMirror),
		Pub1:    pub1,
		Pub2:    pub2,
		Perl2:   strconv.FormatInt(levelZeroPlanet.Perlin, 10),
	}
	proof, err := getProofForMoveCircuit(t, moveAssignment)
	assert.NoError(t, err)

	// 2b) Send transaction to Cardinal
	transaction := tx.SendEnergyMsg{
		LocationHashFrom: fromPlanet.LocationHash,
		LocationHashTo:   toPlanet.LocationHash,
		Energy:           1,
		PerlinTo:         levelZeroPlanet.Perlin,
		RadiusTo:         game.WorldConstants.RadiusMax,
		MaxDistance:      500,
		Proof:            proof,
	}
	signedPayload := sign.Transaction{
		PersonaTag: "Player1",
	}
	txHash := tx.SendEnergy.AddToQueue(world, transaction, &signedPayload)

	// 2c) Run tick so that the ship can be processed
	sentTick := world.CurrentTick()
	doTick()

	// 3) Assert that the correct error was thrown in the system
	receipts, _ := world.TestingGetTransactionReceiptsForTick(sentTick)
	assert.Equal(t, eris.Errorf("ship did not have enough energy to arrive at enemy planet").Error(), receipts[0].Errs[0].Error())
	assert.Equal(t, txHash, receipts[0].TxHash)

	err = world.ShutDown()
	assert.NoError(t, err)
}

// 5) Verify that you can send energy to a friendly planet and the energy math works out
func TestSendEnergyToFriendlyPlanet(t *testing.T) {
	// 0) Setup world
	world, doTick := ScaffoldTestWorld(t)
	wCtx := cardinal.TestingWorldToWorldContext(world)
	player1 := "Player1"

	// 1) Create two planets to send from, make them both owned by Player1
	fromPlanetId, fromPlanet, err := CreateMaxEnergyPlanetByLocationHash(world, levelTwoPlanet.LocationHash, levelTwoPlanet.Perlin, player1)
	assert.NoError(t, err)
	toPlanetId, toPlanet, err := CreateMaxEnergyPlanetByLocationHash(world, levelTwoPlanetTwo.LocationHash, levelTwoPlanetTwo.Perlin, player1)
	assert.NoError(t, err)

	// 2) Project the time the ship will arrive
	distance := 15 // I don't have the exact value for this, but this dist should be good to make the test pass
	energySendTick := world.CurrentTick()
	energyArrivalTick := utils.ShipArrivalTick(utils.IntToDec(distance), utils.ScaleDownByTickRate(fromPlanet.Speed), int64(world.CurrentTick()))

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

	// 3b) Project the energy that the sender and receiver will have once the ship arrives
	senderEnergy, recipientEnergy, err := GetPlanetEnergiesAfterSendingEnergy(world, transaction, fromPlanet.EnergyCurrent, player1, energyArrivalTick)
	assert.NoError(t, err)

	// 3c) Send the transaction (energy)
	SendEnergy(world, transaction, player1)
	assert.NoError(t, err)

	// 3d) Run the world so the send is attempted
	for i := int64(0); i <= energyArrivalTick-int64(energySendTick); i++ {
		doTick()
	}

	// 4) Check that energy did not change
	fromPlanetQueried, err := cardinal.GetComponent[component.PlanetComponent](wCtx, fromPlanetId)
	assert.NoError(t, err)
	toPlanetQueried, err := cardinal.GetComponent[component.PlanetComponent](wCtx, toPlanetId)
	assert.NoError(t, err)

	// 5) Assert that the correct error was thrown in the system
	receipts, _ := world.TestingGetTransactionReceiptsForTick(energySendTick)
	assert.Equal(t, 0, len(receipts[0].Errs))
	assert.Equal(t, utils.DecToStr(senderEnergy), utils.DecToStr(fromPlanetQueried.EnergyCurrent))
	assert.Equal(t, utils.DecToStr(recipientEnergy), utils.DecToStr(toPlanetQueried.EnergyCurrent))
	assert.True(t, toPlanetQueried.OwnerPersonaTag == player1)

	err = world.ShutDown()
	assert.NoError(t, err)
}

// 6) Verify that we can send energy and create a valid planet in ECS and conquer it
func TestSendEnergyAndCreatePlanet(t *testing.T) {
	// 0) Setup world
	world, doTick := ScaffoldTestWorld(t)
	wCtx := cardinal.TestingWorldToWorldContext(world)
	player1 := "Player1"

	// 1) Create one planets to send from
	fromPlanetId, fromPlanet, err := CreateMaxEnergyPlanetByLocationHash(world, levelTwoPlanet.LocationHash, levelTwoPlanet.Perlin, player1)
	assert.NoError(t, err)

	// 2) Send energy from non-owned planet to other planet
	// 2a) Generate proof for the transaction
	distance := 15 // I don't have the exact value for this, but this dist should be good to make the test pass
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
		LocationHashTo:   levelTwoPlanetTwo.LocationHash,
		Energy:           1500,
		PerlinTo:         levelTwoPlanetTwo.Perlin,
		RadiusTo:         game.WorldConstants.RadiusMax,
		MaxDistance:      int64(distance),
		Proof:            proof,
	}

	// 2b) Project the energy that the sender and receiver will have once the ship arrives
	energySendTick := world.CurrentTick()
	energyArrivalTick := utils.ShipArrivalTick(utils.IntToDec(distance), utils.ScaleDownByTickRate(fromPlanet.Speed), int64(world.CurrentTick()))
	senderEnergy, recipientEnergy, err := GetPlanetEnergiesAfterSendingEnergy(world, transaction, fromPlanet.EnergyCurrent, player1, energyArrivalTick)
	assert.NoError(t, err)

	// 2c) Send the transaction (energy)
	SendEnergy(world, transaction, player1)
	assert.NoError(t, err)

	// 2d) Run the world so the send is attempted
	for i := int64(0); i <= energyArrivalTick-int64(energySendTick); i++ {
		doTick()
	}

	// 3) Check that energy did not change
	fromPlanetQueried, err := cardinal.GetComponent[component.PlanetComponent](wCtx, fromPlanetId)
	assert.NoError(t, err)
	toPlanetQueried, _ := component.LoadPlanetComponent(levelTwoPlanetTwo.LocationHash)
	assert.NotEqual(t, component.PlanetComponent{}, toPlanetQueried.Component)

	// 4) Assert that the correct error was thrown in the system
	receipts, _ := world.TestingGetTransactionReceiptsForTick(energySendTick)
	assert.Equal(t, 0, len(receipts[0].Errs))
	assert.Equal(t, utils.DecToStr(senderEnergy), utils.DecToStr(fromPlanetQueried.EnergyCurrent))
	assert.Equal(t, utils.DecToStr(recipientEnergy), utils.DecToStr(toPlanetQueried.Component.EnergyCurrent))
	assert.True(t, toPlanetQueried.Component.OwnerPersonaTag == player1)

	err = world.ShutDown()
	assert.NoError(t, err)
}

// 7) Verify that we can send energy and create a valid planet in ECS and just deal damage to it correctly
func TestSendEnergyAndDealDamageToUnclaimedPlanet(t *testing.T) {
	// 0) Setup world
	world, doTick := ScaffoldTestWorld(t)
	wCtx := cardinal.TestingWorldToWorldContext(world)
	player1 := "Player1"

	// 1) Create planet to send energy from
	fromPlanetId, fromPlanet, err := CreateMaxEnergyPlanetByLocationHash(world, levelTwoPlanet.LocationHash, levelTwoPlanet.Perlin, player1)
	assert.NoError(t, err)

	// 2) Send energy from non-owned planet to other planet
	// 2a) Generate proof for the transaction
	distance := 15 // I don't have the exact value for this, but this dist should be good to make the test pass
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
		LocationHashTo:   levelTwoPlanetTwo.LocationHash,
		Energy:           1250,
		PerlinTo:         levelTwoPlanetTwo.Perlin,
		RadiusTo:         game.WorldConstants.RadiusMax,
		MaxDistance:      int64(distance),
		Proof:            proof,
	}

	// 2b) Project the energy that the sender and receiver will have once the ship arrives
	energySendTick := world.CurrentTick()
	energyArrivalTick := utils.ShipArrivalTick(utils.IntToDec(distance), utils.ScaleDownByTickRate(fromPlanet.Speed), int64(world.CurrentTick()))
	senderEnergy, recipientEnergy, err := GetPlanetEnergiesAfterSendingEnergy(world, transaction, fromPlanet.EnergyCurrent, player1, energyArrivalTick)
	assert.NoError(t, err)

	// 2c) Send the transaction (energy)
	SendEnergy(world, transaction, player1)
	assert.NoError(t, err)

	// 2d) Run the world so the send is attempted
	for i := int64(0); i <= energyArrivalTick-int64(energySendTick); i++ {
		doTick()
	}

	// 3) Check that energy did not change
	fromPlanetQueried, err := cardinal.GetComponent[component.PlanetComponent](wCtx, fromPlanetId)
	assert.NoError(t, err)
	toPlanetQueried, _ := component.LoadPlanetComponent(levelTwoPlanetTwo.LocationHash)
	assert.NotEqual(t, component.PlanetComponent{}, toPlanetQueried)

	// 4) Assert that the correct error was thrown in the system
	receipts, _ := world.TestingGetTransactionReceiptsForTick(energySendTick)
	assert.Equal(t, 0, len(receipts[0].Errs))
	assert.Equal(t, utils.DecToStr(senderEnergy), utils.DecToStr(fromPlanetQueried.EnergyCurrent))
	assert.Equal(t, utils.DecToStr(recipientEnergy), utils.DecToStr(toPlanetQueried.Component.EnergyCurrent))
	assert.True(t, toPlanetQueried.Component.OwnerPersonaTag == "")

	err = world.ShutDown()
	assert.NoError(t, err)
}

// 8) Verify that we can send energy and deal damage to a planet owned by someone else
func TestSendEnergyAndDealDamageToEnemyPlanet(t *testing.T) {
	// 0) Setup world
	world, doTick := ScaffoldTestWorld(t)
	wCtx := cardinal.TestingWorldToWorldContext(world)
	player1 := "Player1"
	player2 := "Player2"

	// 1) Create planets to send and receive, Player 2 owns recipient planet
	fromPlanetId, fromPlanet, err := CreateMaxEnergyPlanetByLocationHash(world, levelTwoPlanet.LocationHash, levelTwoPlanet.Perlin, player1)
	assert.NoError(t, err)
	toPlanetId, toPlanet, err := CreateMaxEnergyPlanetByLocationHash(world, levelTwoPlanetTwo.LocationHash, levelTwoPlanetTwo.Perlin, player2)
	assert.NoError(t, err)

	// 2) Send energy from non-owned planet to other planet
	// 2a) Generate proof for the transaction
	distance := 15 // I don't have the exact value for this, but this dist should be good to make the test pass
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
		Energy:           1900,
		PerlinTo:         levelTwoPlanetTwo.Perlin,
		RadiusTo:         game.WorldConstants.RadiusMax,
		MaxDistance:      int64(distance),
		Proof:            proof,
	}

	// 2b) Project the energy that the sender and receiver will have once the ship arrives
	energySendTick := world.CurrentTick()
	energyArrivalTick := utils.ShipArrivalTick(utils.IntToDec(distance), utils.ScaleDownByTickRate(fromPlanet.Speed), int64(world.CurrentTick()))
	senderEnergy, recipientEnergy, err := GetPlanetEnergiesAfterSendingEnergy(world, transaction, fromPlanet.EnergyCurrent, player1, energyArrivalTick)
	assert.NoError(t, err)

	// 2c) Send the transaction (energy)
	SendEnergy(world, transaction, player1)
	assert.NoError(t, err)

	// 2d) Run the world so the send is attempted
	for i := int64(0); i <= energyArrivalTick-int64(energySendTick); i++ {
		doTick()
	}

	// 3) Check that energy did not change
	fromPlanetQueried, err := cardinal.GetComponent[component.PlanetComponent](wCtx, fromPlanetId)
	assert.NoError(t, err)
	toPlanetQueried, err := cardinal.GetComponent[component.PlanetComponent](wCtx, toPlanetId)
	assert.NoError(t, err)

	// 4) Assert that the correct error was thrown in the system
	receipts, _ := world.TestingGetTransactionReceiptsForTick(energySendTick)
	assert.Equal(t, 0, len(receipts[0].Errs))
	assert.Equal(t, utils.DecToStr(senderEnergy), utils.DecToStr(fromPlanetQueried.EnergyCurrent))
	assert.Equal(t, utils.DecToStr(recipientEnergy), utils.DecToStr(toPlanetQueried.EnergyCurrent))
	assert.True(t, toPlanetQueried.OwnerPersonaTag == player2)

	err = world.ShutDown()
	assert.NoError(t, err)
}

// 9) Verify that we can send energy and conquer an enemy planet
func TestSendEnergyAndConquerPlanet(t *testing.T) {
	// 0) Setup world
	world, doTick := ScaffoldTestWorld(t)
	wCtx := cardinal.TestingWorldToWorldContext(world)
	player1 := "Player1"
	player2 := "Player2"

	// 1) Create planets to send and receive, Player 2 owns recipient planet
	fromPlanetId, fromPlanet, err := CreateMaxEnergyPlanetByLocationHash(world, levelTwoPlanet.LocationHash, levelTwoPlanet.Perlin, player1)
	assert.NoError(t, err)
	toPlanetId, toPlanet, err := CreatePlanetByLocationHash(world, levelTwoPlanetTwo.LocationHash, levelTwoPlanetTwo.Perlin, player2)
	assert.NoError(t, err)

	// 2) Send energy from non-owned planet to other planet
	// 2a) Generate proof for the transaction
	distance := 15 // I don't have the exact value for this, but this dist should be good to make the test pass
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
		Energy:           1700,
		PerlinTo:         levelTwoPlanetTwo.Perlin,
		RadiusTo:         game.WorldConstants.RadiusMax,
		MaxDistance:      int64(distance),
		Proof:            proof,
	}

	// 2b) Project the energy that the sender and receiver will have once the ship arrives
	energySendTick := world.CurrentTick()
	energyArrivalTick := utils.ShipArrivalTick(utils.IntToDec(distance), utils.ScaleDownByTickRate(fromPlanet.Speed), int64(world.CurrentTick()))
	senderEnergy, recipientEnergy, err := GetPlanetEnergiesAfterSendingEnergy(world, transaction, fromPlanet.EnergyCurrent, player1, energyArrivalTick)
	assert.NoError(t, err)

	// 2c) Send the transaction (energy)
	SendEnergy(world, transaction, player1)
	assert.NoError(t, err)

	// 2d) Run the world so the send is attempted
	for i := int64(0); i <= energyArrivalTick-int64(energySendTick); i++ {
		doTick()
	}

	// 3) Check that energy did not change
	fromPlanetQueried, err := cardinal.GetComponent[component.PlanetComponent](wCtx, fromPlanetId)
	assert.NoError(t, err)
	toPlanetQueried, err := cardinal.GetComponent[component.PlanetComponent](wCtx, toPlanetId)
	assert.NoError(t, err)

	// 4) Assert that the correct error was thrown in the system
	receipts, _ := world.TestingGetTransactionReceiptsForTick(energySendTick)
	assert.Equal(t, 0, len(receipts[0].Errs))
	assert.Equal(t, utils.DecToStr(senderEnergy), utils.DecToStr(fromPlanetQueried.EnergyCurrent))
	assert.Equal(t, utils.DecToStr(recipientEnergy), utils.DecToStr(toPlanetQueried.EnergyCurrent))
	assert.True(t, toPlanetQueried.OwnerPersonaTag == player1)

	err = world.ShutDown()
	assert.NoError(t, err)
}
