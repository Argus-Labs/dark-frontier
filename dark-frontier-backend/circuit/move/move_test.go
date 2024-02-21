package move

import (
	"github.com/argus-labs/darkfrontier-backend/circuit"
	"math/big"
	"testing"
	"time"

	"github.com/argus-labs/darkfrontier-backend/circuit/perlin"

	"github.com/consensys/gnark-crypto/ecc"
	"github.com/consensys/gnark/backend"
	"github.com/consensys/gnark/backend/groth16"
	"github.com/consensys/gnark/frontend"
	"github.com/consensys/gnark/frontend/cs/r1cs"
	"github.com/consensys/gnark/test"
)

var (
	moveCircuit MoveCircuit
	// pub1 and pub2 are valid planets that are within range of each other
	pub1 *big.Int
	pub2 *big.Int
	// pub3 and pub4 are valid planets, pub4 is out of the range of pub3
	pub3 *big.Int
	pub4 *big.Int
)

func init() {
	moveCircuit.PlanetHashKey = circuit.PlanetHashKey
	moveCircuit.SpaceTypeKey = circuit.SpaceTypeKey

	var ok bool
	pub1, ok = new(big.Int).SetString("0d01f8778431d6f04310bf3f02fb6e85a173624241fc8d8185c528306f57ca68", 16)
	if !ok {
		panic("failed to parse big.Int from MiMCSharp string output")
	}
	pub2, ok = new(big.Int).SetString("0d00974b8d6df596c0eaf0c8fb15a45dc2da397c3f2d2107661feea02d200e27", 16)
	if !ok {
		panic("failed to parse big.Int from MiMCSharp string output")
	}
	pub3, ok = new(big.Int).SetString("210069a8d887e0f67f934423e7e937f95b6e082e890ce974a505e0d469b18fe1", 16)
	if !ok {
		panic("failed to parse big.Int from MiMCSharp string output")
	}
	pub4, ok = new(big.Int).SetString("06001a8b03144bcc3268aced2193726c7fd86bd03a71dda3206b3c105cb2dca4", 16)
	if !ok {
		panic("failed to parse big.Int from MiMCSharp string output")
	}
}

func TestMove(t *testing.T) {
	assert := test.NewAssert(t)

	assert.ProverSucceeded(
		&moveCircuit,
		&MoveCircuit{
			X1:            3,
			Y1:            -3,
			X2:            11,
			Y2:            -10,
			SpaceTypeKey:  circuit.SpaceTypeKey,
			PlanetHashKey: circuit.PlanetHashKey,
			R:             circuit.RadiusMax,
			DistMax:       53,
			Scale:         circuit.Scale,
			XMirror:       circuit.XMirror,
			YMirror:       circuit.YMirror,
			Pub1:          pub1,
			Pub2:          pub2,
			Perl2:         16,
		},
		test.WithProverOpts(backend.WithHints(perlin.ModuloHint)),
		test.WithBackends(backend.GROTH16),
		test.WithCurves(ecc.BN254),
	)
}

func TestMoveFuzz(t *testing.T) {
	assert := test.NewAssert(t)

	assert.Fuzz(
		&moveCircuit,
		100,
		test.WithProverOpts(backend.WithHints(perlin.ModuloHint)),
		test.WithBackends(backend.GROTH16),
		test.WithCurves(ecc.BN254),
	)
}

// Test whether changing any of the X or Y values without changing the respective
// Pub1 or Pub2 variable will throw an error because of the following constraints:
// MiMCSponge(x1,y1) = pub1, MiMCSponge(x2,y2) = pub2
func TestMiMCConstraint(t *testing.T) {
	assert := test.NewAssert(t)

	assert.ProverFailed(
		&moveCircuit,
		&MoveCircuit{
			X1:            3,
			Y1:            -4, // Changed from -3 to -4
			X2:            11,
			Y2:            -10,
			SpaceTypeKey:  circuit.SpaceTypeKey,
			PlanetHashKey: circuit.PlanetHashKey,
			R:             circuit.RadiusMax,
			DistMax:       53,
			Scale:         circuit.Scale,
			XMirror:       circuit.XMirror,
			YMirror:       circuit.YMirror,
			Pub1:          pub1,
			Pub2:          pub2,
			Perl2:         16,
		},
		test.WithProverOpts(backend.WithHints(perlin.ModuloHint)),
		test.WithBackends(backend.GROTH16),
		test.WithCurves(ecc.BN254),
	)
}

///////////////////////////////////////
//         BOUNDARY TESTS            //
///////////////////////////////////////

// Test that the circuit passes when the distance between p1 and p2 meets:
// (x1-x2)^2 + (y1-y2)^2 <= distMax^2
func TestMoveDistanceWithinBounds(t *testing.T) {
	assert := test.NewAssert(t)

	assert.ProverSucceeded(
		&moveCircuit,
		&MoveCircuit{
			X1:            3,
			Y1:            -3,
			X2:            11,
			Y2:            -10,
			SpaceTypeKey:  circuit.SpaceTypeKey,
			PlanetHashKey: circuit.PlanetHashKey,
			R:             circuit.RadiusMax,
			DistMax:       53,
			Scale:         circuit.Scale,
			XMirror:       circuit.XMirror,
			YMirror:       circuit.YMirror,
			Pub1:          pub1,
			Pub2:          pub2,
			Perl2:         16,
		},
		test.WithProverOpts(backend.WithHints(perlin.ModuloHint)),
		test.WithBackends(backend.GROTH16),
		test.WithCurves(ecc.BN254),
	)
}

// Test that the circuit fails when the distance between p1 and p2 is too great:
// (x1-x2)^2 + (y1-y2)^2 > distMax^2
func TestMoveDistanceTooGreat(t *testing.T) {
	assert := test.NewAssert(t)

	assert.ProverFailed(
		&moveCircuit,
		&MoveCircuit{
			X1:            -3,
			Y1:            25,
			X2:            -23,
			Y2:            7,
			SpaceTypeKey:  circuit.SpaceTypeKey,
			PlanetHashKey: circuit.PlanetHashKey,
			R:             circuit.RadiusMax,
			DistMax:       3,
			Scale:         circuit.Scale,
			XMirror:       circuit.XMirror,
			YMirror:       circuit.YMirror,
			Pub1:          pub3,
			Pub2:          pub4,
			Perl2:         16,
		},
		test.WithProverOpts(backend.WithHints(perlin.ModuloHint)),
		test.WithBackends(backend.GROTH16),
		test.WithCurves(ecc.BN254),
	)
}

// Test that the circuit fails when the new point is outside the circular boundary:
// x2^2 + y2^2 > r^2
// Note: This test doesn't quite work because the planet we are using is not a "valid"
// planet because I do not have a way to easily get a planet from the client that is "out of bounds"
// therefore, the test is passing, but not testing the exact thing we'd want to test.
func TestMoveNewPointOutsideCircularBoundary(t *testing.T) {
	assert := test.NewAssert(t)

	assert.ProverFailed(
		&moveCircuit,
		&MoveCircuit{
			X1:            3,
			Y1:            -3,
			X2:            11,
			Y2:            -10,
			SpaceTypeKey:  circuit.SpaceTypeKey,
			PlanetHashKey: circuit.PlanetHashKey,
			R:             2, // Set radius very low so it fails
			DistMax:       53,
			Scale:         circuit.Scale,
			XMirror:       circuit.XMirror,
			YMirror:       circuit.YMirror,
			Pub1:          pub1,
			Pub2:          pub2,
			Perl2:         16,
		},
		test.WithProverOpts(backend.WithHints(perlin.ModuloHint)),
		test.WithBackends(backend.GROTH16),
		test.WithCurves(ecc.BN254),
	)
}

///////////////////////////////////////
//       INVALID INPUT TESTS         //
///////////////////////////////////////

func TestMoveInvalidKeys(t *testing.T) {
	assert := test.NewAssert(t)

	var newMoveCircuit MoveCircuit
	newMoveCircuit.PlanetHashKey = "invalid_key"
	newMoveCircuit.SpaceTypeKey = "invalid_key"

	// Test that the circuit fails with invalid values for PlanetHashKey and SpaceTypeKey
	assert.ProverFailed(
		&newMoveCircuit,
		&MoveCircuit{
			X1:            3,
			Y1:            -3,
			X2:            11,
			Y2:            -10,
			SpaceTypeKey:  circuit.SpaceTypeKey,
			PlanetHashKey: circuit.PlanetHashKey,
			R:             circuit.RadiusMax,
			DistMax:       53,
			Scale:         circuit.Scale,
			XMirror:       circuit.XMirror,
			YMirror:       circuit.YMirror,
			Pub1:          pub1,
			Pub2:          pub2,
			Perl2:         16,
		},
		test.WithProverOpts(backend.WithHints(perlin.ModuloHint)),
		test.WithBackends(backend.GROTH16),
		test.WithCurves(ecc.BN254),
	)
}

// Test that the circuit fails with an Invalid Scale input:
// Scale = 16385 (not power of 2 and greater than the 16384 boundary)
func TestMoveInvalidScale(t *testing.T) {
	assert := test.NewAssert(t)

	assert.ProverFailed(
		&moveCircuit,
		&MoveCircuit{
			X1:            3,
			Y1:            -3,
			X2:            11,
			Y2:            10,
			SpaceTypeKey:  circuit.SpaceTypeKey,
			PlanetHashKey: circuit.PlanetHashKey,
			R:             circuit.RadiusMax,
			DistMax:       53,
			Scale:         16835,
			XMirror:       circuit.XMirror,
			YMirror:       circuit.YMirror,
			Pub1:          pub1,
			Pub2:          pub2,
			Perl2:         16,
		},
		test.WithProverOpts(backend.WithHints(perlin.ModuloHint)),
		test.WithBackends(backend.GROTH16),
		test.WithCurves(ecc.BN254),
	)
}

func BenchmarkMove(b *testing.B) {
	// NOTE: PlanetHashKey and SpaceTypeKey needs to be initialized here
	// the circuit will compile with nil value
	var newMoveCircuit MoveCircuit
	newMoveCircuit.PlanetHashKey = circuit.PlanetHashKey
	newMoveCircuit.SpaceTypeKey = circuit.SpaceTypeKey

	// Compile circuit
	ccs, err := frontend.Compile(ecc.BN254.ScalarField(), r1cs.NewBuilder, &newMoveCircuit)
	if err != nil {
		return
	}

	// Perform trusted setup
	pk, vk, err := groth16.Setup(ccs)
	if err != nil {
		return
	}

	// Parse MIMC Hash
	pub1, ok := new(big.Int).SetString("0d01f8778431d6f04310bf3f02fb6e85a173624241fc8d8185c528306f57ca68", 16)
	if !ok {
		b.Fatal("failed to parse big.Int from MiMCSharp string output")
	}
	pub2, ok := new(big.Int).SetString("0d00974b8d6df596c0eaf0c8fb15a45dc2da397c3f2d2107661feea02d200e27", 16)
	if !ok {
		b.Fatal("failed to parse big.Int from MiMCSharp string output")
	}

	assignment := &MoveCircuit{
		X1:            3,
		Y1:            -3,
		X2:            11,
		Y2:            10,
		SpaceTypeKey:  circuit.SpaceTypeKey,
		PlanetHashKey: circuit.PlanetHashKey,
		R:             circuit.RadiusMax,
		DistMax:       53,
		Scale:         circuit.Scale,
		XMirror:       circuit.XMirror,
		YMirror:       circuit.YMirror,
		Pub1:          pub1,
		Pub2:          pub2,
		Perl2:         16,
	}

	// BENCH: Witness generation
	startTime := time.Now()
	fullWitness, err := frontend.NewWitness(assignment, ecc.BN254.ScalarField())
	if err != nil {
		return
	}
	endTime := time.Now()
	b.Logf("Witness generation took %s", endTime.Sub(startTime))

	// DEBUG: Public witness
	publicWitness, err := fullWitness.Public()
	if err != nil {
		return
	}

	// BENCH: Proof generation
	startTime = time.Now()
	proof, err := groth16.Prove(ccs, pk, fullWitness, backend.WithHints(perlin.ModuloHint))
	if err != nil {
		return
	}
	endTime = time.Now()
	b.Logf("Proof generation took %s", endTime.Sub(startTime))

	// BENCH: Proof verification
	startTime = time.Now()
	err = groth16.Verify(proof, vk, publicWitness)
	if err != nil {
		return
	}
	endTime = time.Now()
	b.Logf("Proof verification took %s", endTime.Sub(startTime))
}
