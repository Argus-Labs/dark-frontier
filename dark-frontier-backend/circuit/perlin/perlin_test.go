package perlin

import (
	"testing"

	"github.com/consensys/gnark-crypto/ecc"
	"github.com/consensys/gnark/backend"
	"github.com/consensys/gnark/frontend"
	"github.com/consensys/gnark/test"
)

type IsNegativeCircuit struct {
	In frontend.Variable
}

func (circuit *IsNegativeCircuit) Define(api frontend.API) error {
	api.AssertIsEqual(IsNegative(api, circuit.In), 1)
	return nil
}

func TestIsNegativeBasic(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit IsNegativeCircuit

	assert.ProverSucceeded(
		&circuit,
		&IsNegativeCircuit{
			In: -1,
		},
		test.NoFuzzing(),
	)

	assert.ProverFailed(
		&circuit,
		&IsNegativeCircuit{
			In: 1,
		},
		test.NoFuzzing(),
	)
}

func TestNegativeFuzz(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit IsNegativeCircuit

	assert.Fuzz(&circuit, 100)
}

type ModuloCircuit struct {
	Dividend frontend.Variable
	Divisor  frontend.Variable
}

func (circuit *ModuloCircuit) Define(api frontend.API) error {
	Modulo(
		api,
		circuit.Dividend,
		circuit.Divisor,
	)
	return nil
}

func TestBasicModulo(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit ModuloCircuit

	assert.ProverSucceeded(
		&circuit,
		&ModuloCircuit{
			Dividend: -1562582203808432128,
			Divisor:  1125899906842624000,
		},
		test.WithProverOpts(backend.WithHints(ModuloHint)),
		test.NoFuzzing(),
		test.WithCurves(ecc.BN254),
		test.WithBackends(backend.GROTH16),
	)

	assert.ProverFailed(
		&circuit,
		&ModuloCircuit{
			Dividend: 1,
			Divisor:  0,
		},
		test.WithProverOpts(backend.WithHints(ModuloHint)),
		test.NoFuzzing(),
		test.WithCurves(ecc.BN254),
		test.WithBackends(backend.GROTH16),
	)

	assert.ProverSucceeded(
		&circuit,
		&ModuloCircuit{
			Dividend: 0,
			Divisor:  1,
		},
		test.WithProverOpts(backend.WithHints(ModuloHint)),
		test.NoFuzzing(),
		test.WithCurves(ecc.BN254),
		test.WithBackends(backend.GROTH16),
	)
}

func TestModuloFuzz(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit ModuloCircuit

	assert.Fuzz(
		&circuit,
		100,
		test.WithProverOpts(backend.WithHints(ModuloHint)),
	)
}

type RandomGradientAtCircuit struct {
	Denominator frontend.Variable
	In          [2]frontend.Variable
	Scale       frontend.Variable
}

func (circuit *RandomGradientAtCircuit) Define(api frontend.API) error {
	_, err := RandomGradientAt(
		api,
		circuit.Denominator,
		circuit.In,
		circuit.Scale,
		"7",
	)
	return err
}

func TestRandomGradientAtBasic(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit RandomGradientAtCircuit

	assert.ProverSucceeded(
		&circuit,
		// X = "21888242871839275222246405745257275088548364400416034343698204186575808494191"
		// => X = -1426 in BN254
		&RandomGradientAtCircuit{
			Denominator: 1125899906842624000,
			In:          [2]frontend.Variable{-1426, 361},
			Scale:       4096,
		},
		test.NoFuzzing(),
	)
}

func TestRandomGradientAtFuzz(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit RandomGradientAtCircuit

	assert.Fuzz(
		&circuit,
		100,
	)
}

type GetCornersAndGradVectorsCircuit struct {
	Denominator frontend.Variable
	P           [2]frontend.Variable
	Scale       frontend.Variable
}

func (circuit *GetCornersAndGradVectorsCircuit) Define(api frontend.API) error {
	_, _, err := GetCornersAndGradVectors(
		api,
		circuit.Denominator,
		circuit.P,
		circuit.Scale,
		"7",
	)
	return err
}

func TestGetCornersAndGradVectorsBasic(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit GetCornersAndGradVectorsCircuit

	assert.ProverSucceeded(
		&circuit,
		// X = "21888242871839275222246405745257275088548364400416034343698204186575808494191"
		// => X = -1426 in BN254
		&GetCornersAndGradVectorsCircuit{
			Denominator: 1125899906842624000,
			P:           [2]frontend.Variable{-1426, 361},
			Scale:       4096,
		},
		test.WithProverOpts(backend.WithHints(ModuloHint)),
		test.NoFuzzing(),
	)
}

func TestGetCornersAndGradVectorsFuzz(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit GetCornersAndGradVectorsCircuit

	assert.Fuzz(
		&circuit,
		100,
		test.WithProverOpts(backend.WithHints(ModuloHint)),
	)
}

// TODO: Unit test Dot, Weight, PerlinValue fns?

type SingleScalePerlinCircuit struct {
	Denominator frontend.Variable
	P           [2]frontend.Variable
	Scale       frontend.Variable
}

func (circuit *SingleScalePerlinCircuit) Define(api frontend.API) error {
	_, err := SingleScalePerlin(
		api,
		circuit.Denominator,
		circuit.P,
		circuit.Scale,
		"7",
	)
	return err
}

func TestSingleScalePerlinBasic(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit SingleScalePerlinCircuit

	assert.ProverSucceeded(
		&circuit,
		// X = "21888242871839275222246405745257275088548364400416034343698204186575808494191"
		// => X = -1426 in BN254
		&SingleScalePerlinCircuit{
			Denominator: 1125899906842624000,
			P:           [2]frontend.Variable{-1426, 361},
			Scale:       4096,
		},
		test.WithProverOpts(backend.WithHints(ModuloHint)),
		test.NoFuzzing(),
	)
}

func TestSingleScalePerlinFuzz(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit SingleScalePerlinCircuit

	assert.Fuzz(
		&circuit,
		100,
		test.WithProverOpts(backend.WithHints(ModuloHint)),
	)
}

type MultiScalePerlinCircuit struct {
	P       [2]frontend.Variable
	Scale   frontend.Variable
	XMirror frontend.Variable
	YMirror frontend.Variable
}

func (circuit *MultiScalePerlinCircuit) Define(api frontend.API) error {
	_, err := MultiScalePerlin(
		api,
		circuit.P,
		circuit.Scale,
		circuit.XMirror,
		circuit.YMirror,
		"7",
	)
	return err
}

func TestMultiScalePerlinBasic(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit MultiScalePerlinCircuit

	assert.ProverSucceeded(
		&circuit,
		// X = "21888242871839275222246405745257275088548364400416034343698204186575808494191"
		// => X = -1426 in BN254
		&MultiScalePerlinCircuit{
			P:       [2]frontend.Variable{-1426, 366},
			Scale:   4096,
			XMirror: 1,
			YMirror: 1,
		},
		test.WithProverOpts(backend.WithHints(ModuloHint)),
		test.NoFuzzing(),
		test.WithCurves(ecc.BN254),
		// api.Cmp panics on parsing denominator variable when using PLONKFRI...
		test.WithBackends(backend.GROTH16),
	)
}

func TestMultiScalePerlinFuzz(t *testing.T) {
	assert := test.NewAssert(t)

	var circuit MultiScalePerlinCircuit

	assert.Fuzz(
		&circuit,
		100,
		test.WithProverOpts(backend.WithHints(ModuloHint)),
		test.WithBackends(backend.GROTH16),
	)
}
