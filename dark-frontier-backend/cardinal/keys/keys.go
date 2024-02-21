package keys

import (
	"bytes"
	"github.com/argus-labs/darkfrontier-backend/circuit/artifacts"
	"github.com/consensys/gnark-crypto/ecc"
	"github.com/consensys/gnark/backend/groth16"
)

var (
	InitVerifyingKey = groth16.NewVerifyingKey(ecc.BN254)
	MoveVerifyingKey = groth16.NewVerifyingKey(ecc.BN254)
)

func init() {
	initVkBytes := artifacts.InitVerifyingKey
	_, err := InitVerifyingKey.ReadFrom(bytes.NewReader(initVkBytes))
	if err != nil {
		panic(err)
	}
	moveVkBytes := artifacts.MoveVerifyingKey
	_, err = MoveVerifyingKey.ReadFrom(bytes.NewReader(moveVkBytes))
	if err != nil {
		panic(err)
	}
}
