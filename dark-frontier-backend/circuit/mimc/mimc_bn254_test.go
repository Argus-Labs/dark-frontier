package mimcbn254

import (
	"encoding/json"
	"math/big"
	"os"
	"testing"

	"github.com/consensys/gnark-crypto/ecc"
	"github.com/consensys/gnark/backend"
	"github.com/consensys/gnark/frontend"
	"github.com/consensys/gnark/test"
)

type MiMCBN254BasicCircuit struct {
	Input [2]frontend.Variable
}

func (circuit *MiMCBN254BasicCircuit) Define(api frontend.API) error {
	mimc, err := NewMiMC(api, "seed", 110)
	if err != nil {
		return err
	}
	mimc.Write(circuit.Input[:]...)
	res := mimc.Sum()
	api.Println(res)
	return nil
}

func TestMiMCBN254Basic(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit MiMCBN254BasicCircuit

	assert.ProverSucceeded(
		&circuit,
		&MiMCBN254BasicCircuit{
			Input: [2]frontend.Variable{1764, -3132},
		},
		test.NoFuzzing(),
		test.NoSerialization(),
		test.WithCurves(ecc.BN254),
		test.WithBackends(backend.GROTH16),
	)
}

type TestVector struct {
	In  []string `json:"in"`
	Out string   `json:"out"`
}

type MiMCBN254TestVectorCircuit struct {
	Input  []frontend.Variable
	Output frontend.Variable
}

func (circuit *MiMCBN254TestVectorCircuit) Define(api frontend.API) error {
	mimc, err := NewMiMC(api, "seed", 110)
	if err != nil {
		return err
	}
	mimc.Write(circuit.Input...)
	res := mimc.Sum()
	api.AssertIsEqual(res, circuit.Output)
	return nil
}

func TestMiMCBN254Vectors(t *testing.T) {
	assert := test.NewAssert(t)

	// Parse test vectors (taken from https://github.com/ConsenSys/gnark-crypto/blob/master/ecc/bn254/fr/mimc/test_vectors/vectors.json)
	vectorsContent, err := os.ReadFile("./vectors.json")
	if err != nil {
		t.Fatal("failed to open test vector file")
	}

	var testVectors []TestVector
	err = json.Unmarshal(vectorsContent, &testVectors)
	if err != nil {
		t.Fatal("failed to unmarshal test vector")
	}

	for _, testVector := range testVectors {
		var circuit MiMCBN254TestVectorCircuit
		circuit.Input = make([]frontend.Variable, len(testVector.In))

		// Parse variables from testVector
		input := make([]frontend.Variable, len(testVector.In))
		for i, inHex := range testVector.In {
			// Can use base 0 because strings contain "0x" prefix
			inputBigInt, ok := new(big.Int).SetString(inHex, 0)
			if !ok {
				t.Fatal("failed to parse big.Int from input")
			}
			input[i] = *inputBigInt
		}
		output, ok := new(big.Int).SetString(testVector.Out, 0)
		if !ok {
			t.Fatal("failed to parse big.Int from output")
		}

		assert.ProverSucceeded(
			&circuit,
			&MiMCBN254TestVectorCircuit{
				Input:  input,
				Output: output,
			},
			test.NoFuzzing(),
			test.NoSerialization(),
			test.WithCurves(ecc.BN254),
		)
	}

}
