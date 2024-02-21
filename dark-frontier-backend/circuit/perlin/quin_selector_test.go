package perlin

import (
	"testing"

	"github.com/consensys/gnark/frontend"
	"github.com/consensys/gnark/test"
)

type IsEqualCircuit struct {
	A frontend.Variable
	B frontend.Variable
}

func (circuit *IsEqualCircuit) Define(api frontend.API) error {
	api.AssertIsEqual(
		IsEqual(api, circuit.A, circuit.B),
		1,
	)

	return nil
}

func TestIsEqualBasic(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit IsEqualCircuit

	assert.ProverSucceeded(
		&circuit,
		&IsEqualCircuit{
			A: 1,
			B: 1,
		},
		test.NoFuzzing(),
	)

	assert.ProverFailed(
		&circuit,
		&IsEqualCircuit{
			A: 1,
			B: 0,
		},
		test.NoFuzzing(),
	)
}

func TestIsEqualFuzz(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit IsEqualCircuit

	assert.Fuzz(&circuit, 100)
}

type QuinSelectorCircuit struct {
	Index   frontend.Variable
	Choices [3]frontend.Variable
}

func (circuit *QuinSelectorCircuit) Define(api frontend.API) error {
	QuinSelector(api, circuit.Index, circuit.Choices[:])
	return nil
}

func TestQuinSelectorBasic(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit QuinSelectorCircuit

	choices := [3]frontend.Variable{2, 3, 4}

	assert.ProverSucceeded(
		&circuit,
		&QuinSelectorCircuit{
			Index:   1,
			Choices: choices,
		},
		test.NoFuzzing(),
	)

	assert.ProverFailed(
		&circuit,
		&QuinSelectorCircuit{
			Index:   3,
			Choices: choices,
		},
		test.NoFuzzing(),
	)
}

func TestQuinSelectorFuzz(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit QuinSelectorCircuit

	assert.Fuzz(
		&circuit,
		100,
	)
}
