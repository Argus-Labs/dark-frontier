package initialize

import (
	"math/big"

	mimcbn254 "github.com/argus-labs/darkfrontier-backend/circuit/mimc"
	"github.com/argus-labs/darkfrontier-backend/circuit/perlin"

	"github.com/consensys/gnark/frontend"
)

type InitCircuit struct {
	X             frontend.Variable
	Y             frontend.Variable
	R             frontend.Variable
	PlanetHashKey string
	SpaceTypeKey  string
	Scale         frontend.Variable `gnark:",public"`
	XMirror       frontend.Variable `gnark:",public"`
	YMirror       frontend.Variable `gnark:",public"`
	Pub           frontend.Variable `gnark:",public"`
	Perl          frontend.Variable `gnark:",public"`
}

func (circuit *InitCircuit) Define(api frontend.API) error {
	pub, perl, err := Init(
		api,
		circuit.X,
		circuit.Y,
		circuit.R,
		circuit.PlanetHashKey,
		circuit.SpaceTypeKey,
		circuit.Scale,
		circuit.XMirror,
		circuit.YMirror,
	)

	////////////////////////////
	// Check MiMC(x, y) = pub //
	////////////////////////////
	api.AssertIsEqual(pub, circuit.Pub)

	///////////////////////////////
	// Check perlin(x, y) = perl //
	///////////////////////////////
	api.AssertIsEqual(perl, circuit.Perl)

	return err
}

// Prove: I know (x,y) such that:
// - x^2 + y^2 <= r^2
// - perlin(x, y) = perl
// - MiMC(x,y) = pub
func Init(
	api frontend.API,
	x frontend.Variable,
	y frontend.Variable,
	r frontend.Variable,
	planetHashKey string,
	spaceTypeKey string,
	// Must be a power of 2, at most 16384, so that DENOMINATOR works
	scale frontend.Variable,
	// 1 is true, 0 is false
	xMirror frontend.Variable,
	// 1 is true, 0 is false
	yMirror frontend.Variable,
) (frontend.Variable, frontend.Variable, error) {
	//////////////////////////////////
	// Check abs(x), abs(y) <= 2^31 //
	//////////////////////////////////
	lShift := big.NewInt(1)
	lShift.Lsh(lShift, uint(31))
	api.ToBinary(api.Add(x, lShift), 32)
	api.ToBinary(api.Add(y, lShift), 32)

	///////////////////////////
	// Check x^2 + y^2 < r^2 //
	///////////////////////////
	xSq := api.Mul(x, x)
	ySq := api.Mul(y, y)
	rSq := api.Mul(r, r)

	// We use this value a couple times, so defining it here to
	// avoid duplicate variable declaration.
	xSqPlusYSq := api.Add(xSq, ySq)

	// Circom circuit uses a LessThan check over 64-bit numbers
	// Need to constrain that rSq fits in 64 bits
	// (already constrained for xSq & ySq)
	api.ToBinary(rSq, 64)
	api.AssertIsLessOrEqual(
		xSqPlusYSq,
		api.Sub(rSq, 1),
	)

	// When enabled, player can only spawn in the edges of the radius
	///////////////////////////////////////////////
	// Check x^2 + y^2 > 0.98 * r^2              //
	// Equivalently 100 * (x^2 + y^2) > 98 * r^2 //
	///////////////////////////////////////////////
	// api.AssertIsLessOrEqual(
	// 	api.Mul(rSq, 98),
	// 	api.Sub(api.Mul(xSqPlusYSq, 100), 1),
	// )

	////////////////////////////////////////////////////////////////////////////////////////
	// check MiMCSponge(x,y) = pub                                                        //
	//                                                                                    //
	//  220 = 2 * ceil(log_5 p), as specified by mimc paper, where                        //
	//  p = 21888242871839275222246405745257275088548364400416034343698204186575808495617 //
	//                                                                                    //
	////////////////////////////////////////////////////////////////////////////////////////

	// Circom MiMCSponge circuit uses 220 rounds b/c it uses MiMC-2n/n construction,
	// this uses MiMC-n/n construction so will use 110 rounds
	// Calculate mimc hash
	mimc, err := mimcbn254.NewMiMC(api, planetHashKey, 110)
	if err != nil {
		return nil, nil, err
	}
	mimc.Write(x, y)
	pub := mimc.Sum()

	// Calculate perlin value
	perl, err := perlin.MultiScalePerlin(
		api,
		[2]frontend.Variable{x, y},
		scale,
		xMirror,
		yMirror,
		spaceTypeKey,
	)
	if err != nil {
		return nil, nil, err
	}

	return pub, perl, nil
}
