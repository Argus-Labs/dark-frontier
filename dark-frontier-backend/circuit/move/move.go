package move

import (
	"math/big"

	mimcbn254 "github.com/argus-labs/darkfrontier-backend/circuit/mimc"
	"github.com/argus-labs/darkfrontier-backend/circuit/perlin"

	"github.com/consensys/gnark/frontend"
)

type MoveCircuit struct {
	X1            frontend.Variable
	Y1            frontend.Variable
	X2            frontend.Variable
	Y2            frontend.Variable
	PlanetHashKey string
	SpaceTypeKey  string
	R             frontend.Variable `gnark:",public"`
	DistMax       frontend.Variable `gnark:",public"`
	Scale         frontend.Variable `gnark:",public"` // Power of 2, Max 16384
	XMirror       frontend.Variable `gnark:",public"` // 1 is true, 0 is false
	YMirror       frontend.Variable `gnark:",public"` // 1 is true, 0 is false
	Pub1          frontend.Variable `gnark:",public"`
	Pub2          frontend.Variable `gnark:",public"`
	Perl2         frontend.Variable `gnark:",public"`
}

func (circuit *MoveCircuit) Define(api frontend.API) error {
	pub1, pub2, perl2, err := Move(
		api,
		circuit.X1,
		circuit.Y1,
		circuit.X2,
		circuit.Y2,
		circuit.PlanetHashKey,
		circuit.SpaceTypeKey,
		circuit.R,
		circuit.DistMax,
		circuit.Scale,
		circuit.XMirror,
		circuit.YMirror,
	)

	///////////////////////////////
	// Check MiMC(x1, y1) = pub1 //
	///////////////////////////////
	api.AssertIsEqual(pub1, circuit.Pub1)

	///////////////////////////////
	// Check MiMC(x2, y2) = pub2 //
	///////////////////////////////
	api.AssertIsEqual(pub2, circuit.Pub2)

	////////////////////////////////
	// Check perlin(x, y) = perl2 //
	////////////////////////////////
	api.AssertIsEqual(perl2, circuit.Perl2)

	return err
}

func Move(
	api frontend.API,
	x1 frontend.Variable,
	y1 frontend.Variable,
	x2 frontend.Variable,
	y2 frontend.Variable,
	planetHashKey string,
	spaceTypeKey string,
	r frontend.Variable,
	distMax frontend.Variable,
	scale frontend.Variable,
	xMirror frontend.Variable,
	yMirror frontend.Variable,
) (frontend.Variable, frontend.Variable, frontend.Variable, error) {
	// Prove: I know (x1,y1,x2,y2,p2,r2,distMax) such that:
	// - x2^2 + y2^2 <= r^2
	// - perlin(x2, y2) = perlin2
	// - (x1-x2)^2 + (y1-y2)^2 <= distMax^2
	// - MiMCSponge(x1,y1) = pub1
	// - MiMCSponge(x2,y2) = pub2

	//////////////////////////////////////////////////////
	// Check abs(x1), abs(y1), abs(x2), abs(y2) <= 2^31 //
	//////////////////////////////////////////////////////
	lShift := big.NewInt(1)
	lShift.Lsh(lShift, uint(31))
	api.ToBinary(api.Add(x1, lShift), 32)
	api.ToBinary(api.Add(y1, lShift), 32)
	api.ToBinary(api.Add(x2, lShift), 32)
	api.ToBinary(api.Add(y2, lShift), 32)

	/////////////////////////////
	// Check x2^2 + y2^2 < r^2 //
	/////////////////////////////
	x2Sq := api.Mul(x2, x2)
	y2Sq := api.Mul(y2, y2)
	rSq := api.Mul(r, r)

	// Circom circuit uses a LessThan check over 64-bit numbers
	// Need to constrain that rSq fits in 64 bits
	// (already constrained for x2Sq & y2Sq)
	api.ToBinary(rSq, 64)
	api.AssertIsLessOrEqual(
		api.Add(x2Sq, y2Sq),
		api.Sub(rSq, 1),
	)

	//////////////////////////////////////////////
	// Check (x1-x2)^2 + (y1-y2)^2 <= distMax^2 //
	//////////////////////////////////////////////
	diffX := api.Sub(x1, x2)
	diffXSq := api.Mul(diffX, diffX)
	diffY := api.Sub(y1, y2)
	diffYSq := api.Mul(diffY, diffY)
	distMaxSq := api.Mul(distMax, distMax)

	// Circom circuit uses a LessThan check over 64-bit numbers
	// Need to constrain that distMax^2 fits in 64 bits
	// (already constrained for x2Sq & y2Sq)
	api.ToBinary(distMaxSq, 64)

	api.AssertIsLessOrEqual(
		api.Add(diffXSq, diffYSq),
		distMaxSq,
	)

	mimc1, err := mimcbn254.NewMiMC(api, planetHashKey, 110)
	if err != nil {
		return nil, nil, nil, err
	}

	mimc1.Write(x1, y1)
	pub1 := mimc1.Sum()

	mimc2, err := mimcbn254.NewMiMC(api, planetHashKey, 110)
	if err != nil {
		return nil, nil, nil, err
	}

	mimc2.Write(x2, y2)
	pub2 := mimc2.Sum()

	perlin2, err := perlin.MultiScalePerlin(
		api,
		[2]frontend.Variable{x2, y2},
		scale,
		xMirror,
		yMirror,
		spaceTypeKey,
	)
	if err != nil {
		return nil, nil, nil, err
	}

	return pub1, pub2, perlin2, nil
}
