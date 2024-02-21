package range_proof

import (
	"testing"

	"github.com/consensys/gnark-crypto/ecc"
	"github.com/consensys/gnark/backend"
	"github.com/consensys/gnark/frontend"
	"github.com/consensys/gnark/test"
)

func TestRangeBasic(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit RangeCircuit

	assert.ProverSucceeded(
		&circuit,
		&RangeCircuit{
			MaxAbsValue: 3,
			In:          2,
		},
		test.NoFuzzing(),
	)

	assert.ProverFailed(
		&circuit,
		&RangeCircuit{
			MaxAbsValue: 2,
			In:          3,
		},
		test.NoFuzzing(),
	)
}

func TestRangeNegative(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit RangeCircuit

	assert.ProverSucceeded(
		&circuit,
		&RangeCircuit{
			MaxAbsValue: 3,
			In:          -2,
		},
		test.NoFuzzing(),
	)

	// Fails for PLONKFRI + BLS24_317 when it shouldn't, issue here:
	// https://github.com/ConsenSys/gnark/issues/523

	curves := []ecc.ID{
		ecc.BN254,
		ecc.BLS12_377,
		ecc.BLS12_381,
		ecc.BLS24_315,
		ecc.BW6_633,
		ecc.BW6_761,
	}

	backends := []backend.ID{
		backend.GROTH16,
		backend.PLONK,
	}

	assert.ProverFailed(
		&circuit,
		&RangeCircuit{
			MaxAbsValue: 2,
			In:          -3,
		},
		test.NoFuzzing(),
		test.WithCurves(curves[0], curves[1:]...),
		test.WithBackends(backends[0], backends[1:]...),
	)

	assert.ProverFailed(
		&circuit,
		&RangeCircuit{
			MaxAbsValue: 100,
			In:          -101,
		},
		test.NoFuzzing(),
		test.WithCurves(curves[0], curves[1:]...),
		test.WithBackends(backends[0], backends[1:]...),
	)
}

func TestRangeFuzz(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit RangeCircuit

	assert.Fuzz(&circuit, 100)
}

type MultiRangeCircuit struct {
	MaxAbsValue frontend.Variable
	In          [3]frontend.Variable
}

func (circuit *MultiRangeCircuit) Define(api frontend.API) error {
	MultiRangeProof(api, 64, circuit.MaxAbsValue, circuit.In[:]...)
	return nil
}

func TestBasicMultiRange(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit MultiRangeCircuit

	in := [3]frontend.Variable{2, 3, 4}

	assert.ProverSucceeded(
		&circuit,
		&MultiRangeCircuit{
			MaxAbsValue: 5,
			In:          in,
		},
		test.NoFuzzing(),
	)

	assert.ProverFailed(
		&circuit,
		&MultiRangeCircuit{
			MaxAbsValue: 3,
			In:          in,
		},
		test.NoFuzzing(),
	)
}

func TestMultiRangeFuzz(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit MultiRangeCircuit

	assert.Fuzz(&circuit, 100)
}
