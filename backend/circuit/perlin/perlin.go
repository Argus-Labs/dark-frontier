package perlin

import (
	"math/big"

	mimcbn254 "github.com/argus-labs/darkfrontier-backend/circuit/mimc"
	"github.com/argus-labs/darkfrontier-backend/circuit/range_proof"

	"github.com/consensys/gnark/frontend"
)

// input: three field elements: x, y, scale (all absolute value < 2^32)
// output: pseudorandom integer in [0, 15]
func Random(
	api frontend.API,
	in [3]frontend.Variable,
	key string,
) (frontend.Variable, error) {
	mimc, err := mimcbn254.NewMiMC(api, key, 4)
	if err != nil {
		return nil, err
	}

	// api.Println("in", in[0], in[1], in[2])

	mimc.Write(in[0], in[1], in[2])

	mimcBits := api.ToBinary(mimc.Sum())

	out := api.Add(
		api.Mul(mimcBits[3], 8),
		api.Mul(mimcBits[2], 4),
		api.Mul(mimcBits[1], 2),
		mimcBits[0],
	)

	return out, nil
}

// 1 if `in` in (p/2, p], 0 otherwise
func IsNegative(api frontend.API, in frontend.Variable) frontend.Variable {
	two := big.NewInt(2)
	res := api.Cmp(in, two.Div(api.Compiler().Field(), two))
	return api.IsZero(api.Sub(res, 1))
}

// Returns in[0] % in[1], assuming both are positive.
// THIS MUST BE PROPERLY CONSTRAINED IN SNARK
func ModuloHint(q *big.Int, inputs []*big.Int, results []*big.Int) error {
	absDividend := inputs[0]
	divisor := inputs[1]
	dividendIsNegative := inputs[2]

	remainder := results[0]

	remainder.Mod(absDividend, divisor)

	if dividendIsNegative.Int64() == 1 && remainder.Sign() != 0 {
		remainder.Sub(divisor, remainder)
	}

	return nil
}

// input: dividend and divisor field elements in [0, sqrt(p))
// output: remainder and quotient field elements in [0, p-1] and [0, sqrt(p)
// Haven't thought about negative divisor yet. Not needed.
// -8 % 5 = 2. [-8 -> 8. 8 % 5 -> 3. 5 - 3 -> 2.]
// (-8 - 2) // 5 = -2
// -8 + 2 * 5 = 2
// check: 2 - 2 * 5 = -8
func Modulo(
	api frontend.API,
	dividend frontend.Variable,
	divisor frontend.Variable,
) (frontend.Variable, frontend.Variable) {
	dividendIsNegative := IsNegative(api, dividend)
	absDividend := api.Mul(
		api.Add(
			api.Mul(dividendIsNegative, -2),
			1,
		),
		dividend,
	)

	moduloResults, _ := api.Compiler().NewHint(ModuloHint, 1, absDividend, divisor, dividendIsNegative)
	remainder := moduloResults[0]

	quotient := api.Div(api.Sub(dividend, remainder), divisor)

	api.AssertIsEqual(
		dividend,
		api.Add(
			api.Mul(
				divisor,
				quotient,
			),
			remainder,
		),
	)

	////////////////////////////////////////////////////////////
	// Check that divisor, quotient, dividend in [0, sqrt(p)) //
	////////////////////////////////////////////////////////////
	sqrtP := big.NewInt(0).Sqrt(api.Compiler().Field())
	range_proof.MultiRangeProof(
		api,
		sqrtP.BitLen(),
		sqrtP,
		dividend,
		divisor,
		quotient,
	)

	/////////////////////////////////////////
	// Check that 0 <= remainder < divisor //
	/////////////////////////////////////////
	api.AssertIsLessOrEqual(remainder, api.Sub(divisor, 1))

	return quotient, remainder
}

// input: three field elements x, y, scale (all absolute value < 2^32)
// output: (NUMERATORS) a random unit vector in one of 16 directions
func RandomGradientAt(
	api frontend.API,
	denominator frontend.Variable,
	in [2]frontend.Variable,
	scale frontend.Variable,
	key string,
) ([2]frontend.Variable, error) {
	vecs := [16][2]frontend.Variable{{1000, 0}, {923, 382}, {707, 707}, {382, 923}, {0, 1000}, {-383, 923}, {-708, 707}, {-924, 382}, {-1000, 0}, {-924, -383}, {-708, -708}, {-383, -924}, {-1, -1000}, {382, -924}, {707, -708}, {923, -383}}

	rand, err := Random(
		api,
		[3]frontend.Variable{in[0], in[1], scale},
		key,
	)
	if err != nil {
		return [2]frontend.Variable{nil, nil}, err
	}

	var xs [16]frontend.Variable
	var ys [16]frontend.Variable
	for i, vec := range vecs {
		xs[i] = vec[0]
		ys[i] = vec[1]
	}

	randX := QuinSelector(api, rand, xs[:])
	randY := QuinSelector(api, rand, ys[:])

	vectorDenominator := api.Div(denominator, 1000)

	// api.Println(randX, randY)

	return [2]frontend.Variable{
		api.Mul(randX, vectorDenominator),
		api.Mul(randY, vectorDenominator),
	}, nil
}

// input: x, y, scale (field elements absolute value < 2^32)
// output: 4 corners of a square with sidelen = scale (INTEGER coords)
// and parallel array of 4 gradient vectors (NUMERATORS)
func GetCornersAndGradVectors(
	api frontend.API,
	denominator frontend.Variable,
	p [2]frontend.Variable,
	scale frontend.Variable,
	key string,
) (
	[4][2]frontend.Variable,
	[4][2]frontend.Variable,
	error,
) {
	nilVars := [4][2]frontend.Variable{
		{nil, nil},
		{nil, nil},
		{nil, nil},
		{nil, nil},
	}

	CornerGrad := func(
		x frontend.Variable,
		y frontend.Variable,
	) ([2]frontend.Variable, error) {
		return RandomGradientAt(
			api,
			denominator,
			[2]frontend.Variable{x, y},
			scale,
			key,
		)
	}

	_, xRemainder := Modulo(
		api,
		p[0],
		scale,
	)

	_, yRemainder := Modulo(
		api,
		p[1],
		scale,
	)

	bottomLeftCoords := [2]frontend.Variable{
		api.Sub(p[0], xRemainder),
		api.Sub(p[1], yRemainder),
	}

	bottomRightCoords := [2]frontend.Variable{
		api.Add(bottomLeftCoords[0], scale),
		bottomLeftCoords[1],
	}
	topLeftCoords := [2]frontend.Variable{
		bottomLeftCoords[0],
		api.Add(bottomLeftCoords[1], scale),
	}
	topRightCoords := [2]frontend.Variable{
		api.Add(bottomLeftCoords[0], scale),
		api.Add(bottomLeftCoords[1], scale),
	}

	bottomLeftGrad, err := CornerGrad(
		bottomLeftCoords[0],
		bottomLeftCoords[1],
	)
	if err != nil {
		return nilVars, nilVars, err
	}
	bottomRightGrad, err := CornerGrad(
		bottomRightCoords[0],
		bottomRightCoords[1],
	)
	if err != nil {
		return nilVars, nilVars, err
	}
	topLeftGrad, err := CornerGrad(
		topLeftCoords[0],
		topLeftCoords[1],
	)
	if err != nil {
		return nilVars, nilVars, err
	}
	topRightGrad, err := CornerGrad(
		topRightCoords[0],
		topRightCoords[1],
	)
	if err != nil {
		return nilVars, nilVars, err
	}

	coords := [4][2]frontend.Variable{
		{bottomLeftCoords[0], bottomLeftCoords[1]},
		{bottomRightCoords[0], bottomRightCoords[1]},
		{topLeftCoords[0], topLeftCoords[1]},
		{topRightCoords[0], topRightCoords[1]},
	}
	grads := [4][2]frontend.Variable{
		{bottomLeftGrad[0], bottomLeftGrad[1]},
		{bottomRightGrad[0], bottomRightGrad[1]},
		{topLeftGrad[0], topLeftGrad[1]},
		{topRightGrad[0], topRightGrad[1]},
	}

	return coords, grads, nil
}

func GetWeight(
	api frontend.API,
	denominator frontend.Variable,
	diff [2]frontend.Variable,
) frontend.Variable {
	return api.Div(
		api.Mul(
			api.Sub(denominator, diff[0]),
			api.Sub(denominator, diff[1]),
		),
		denominator,
	)
}

// input: corner is FRAC NUMERATORS of scale x scale square, scaled down to unit square
// p is FRAC NUMERATORS of a point inside a scale x scale that was scaled down to unit sqrt
// output: FRAC NUMERATOR of weight of the gradient at this corner for this point
func GetWeightBL(
	api frontend.API,
	denominator frontend.Variable,
	corner [2]frontend.Variable,
	p [2]frontend.Variable,
) frontend.Variable {
	return GetWeight(
		api,
		denominator,
		[2]frontend.Variable{
			api.Sub(p[0], corner[0]),
			api.Sub(p[1], corner[1]),
		},
	)
}

func GetWeightBR(
	api frontend.API,
	denominator frontend.Variable,
	corner [2]frontend.Variable,
	p [2]frontend.Variable,
) frontend.Variable {
	return GetWeight(
		api,
		denominator,
		[2]frontend.Variable{
			api.Sub(corner[0], p[0]),
			api.Sub(p[1], corner[1]),
		},
	)
}

func GetWeightTL(
	api frontend.API,
	denominator frontend.Variable,
	corner [2]frontend.Variable,
	p [2]frontend.Variable,
) frontend.Variable {
	return GetWeight(
		api,
		denominator,
		[2]frontend.Variable{
			api.Sub(p[0], corner[0]),
			api.Sub(corner[1], p[1]),
		},
	)
}

func GetWeightTR(
	api frontend.API,
	denominator frontend.Variable,
	corner [2]frontend.Variable,
	p [2]frontend.Variable,
) frontend.Variable {
	return GetWeight(
		api,
		denominator,
		[2]frontend.Variable{
			api.Sub(corner[0], p[0]),
			api.Sub(corner[1], p[1]),
		},
	)
}

// dot product of two vector NUMERATORS
func Dot(
	api frontend.API,
	denominator frontend.Variable,
	a [2]frontend.Variable,
	b [2]frontend.Variable,
) frontend.Variable {
	return api.Div(
		api.Add(
			api.Mul(a[0], b[0]),
			api.Mul(a[1], b[1]),
		),
		denominator,
	)
}

// input: 4 gradient unit vectors (NUMERATORS)
// corner coords of a scale x scale square (ints)
// point inside (int world coords)
func PerlinValue(
	api frontend.API,
	denominator frontend.Variable,
	coords [4][2]frontend.Variable,
	grads [4][2]frontend.Variable,
	scale frontend.Variable,
	p [2]frontend.Variable,
) frontend.Variable {
	weightFns := [4]func(
		api frontend.API,
		denominator frontend.Variable,
		corner [2]frontend.Variable,
		p [2]frontend.Variable,
	) frontend.Variable{
		GetWeightBL,
		GetWeightBR,
		GetWeightTL,
		GetWeightTR,
	}

	total := frontend.Variable(big.NewInt(0))
	for i := 0; i < 4; i++ {
		weight := weightFns[i](
			api,
			denominator,
			[2]frontend.Variable{
				api.Div(coords[i][0], scale),
				api.Div(coords[i][1], scale),
			},
			[2]frontend.Variable{
				api.Div(p[0], scale),
				api.Div(p[1], scale),
			},
		)

		scaledDistVec := [2]frontend.Variable{
			api.Div(
				api.Sub(p[0], coords[i][0]),
				scale,
			),
			api.Div(
				api.Sub(p[1], coords[i][1]),
				scale,
			),
		}

		dot := Dot(
			api,
			denominator,
			[2]frontend.Variable{
				grads[i][0],
				grads[i][1],
			},
			[2]frontend.Variable{
				scaledDistVec[0],
				scaledDistVec[1],
			},
		)

		total = api.MulAcc(
			total,
			api.Div(
				api.Mul(dot, weight),
				denominator,
			),
			1,
		)
	}

	return total
}

func SingleScalePerlin(
	api frontend.API,
	denominator frontend.Variable,
	p [2]frontend.Variable,
	scale frontend.Variable,
	key string,
) (frontend.Variable, error) {
	coords, grads, err := GetCornersAndGradVectors(
		api,
		denominator,
		p,
		scale,
		key,
	)
	if err != nil {
		return nil, err
	}

	denomP := [2]frontend.Variable{
		api.Mul(denominator, p[0]),
		api.Mul(denominator, p[1]),
	}

	var denomCoords [4][2]frontend.Variable
	for i := 0; i < 4; i++ {
		denomCoords[i] = [2]frontend.Variable{
			api.Mul(denominator, coords[i][0]),
			api.Mul(denominator, coords[i][1]),
		}
	}

	return PerlinValue(
		api,
		denominator,
		denomCoords,
		grads,
		scale,
		denomP,
	), nil
}

// TODO(stretch): Make this parameterizable by number of single scale perlin values (currently hardcoded to 3)
func MultiScalePerlin(
	api frontend.API,
	p [2]frontend.Variable,
	// power of 2 at most 16384 so that denominator works
	scale frontend.Variable,
	// 1 is true, 0 is false
	xMirror frontend.Variable,
	// 1 is true, 0 is false
	yMirror frontend.Variable,
	key string,
) (frontend.Variable, error) {
	// good for length scales up to 16384. 2^50 * 1000
	denominator := big.NewInt(1125899906842624000)

	api.AssertIsBoolean(xMirror)
	api.AssertIsBoolean(yMirror)

	lShift := big.NewInt(1)
	lShift.Lsh(lShift, uint(31))
	range_proof.MultiRangeProof(
		api,
		35,
		lShift,
		p[0],
		p[1],
	)

	xAdjusted := api.Mul(
		p[0],
		api.Add(
			api.Mul(
				-2,
				// should flip sign of x coord (p[0]) if yMirror is true (i.e. flip along vertical axis) and p[0] is negative
				api.Mul(
					IsNegative(api, p[0]),
					yMirror,
				),
			),
			1,
		),
	)
	yAdjusted := api.Mul(
		p[1],
		api.Add(
			api.Mul(
				-2,
				// should flip sign of y coord (p[1]) if xMirror is true (i.e. flip along horizontal axis) and p[1] is negative
				api.Mul(
					IsNegative(api, p[1]),
					xMirror,
				),
			),
			1,
		),
	)

	// add perlins[0], perlins[1], perlins[2], and perlins[0] (again)
	firstPerlin, err := SingleScalePerlin(
		api,
		denominator,
		[2]frontend.Variable{xAdjusted, yAdjusted},
		scale,
		key,
	)
	if err != nil {
		return nil, err
	}

	// api.Println("firstPerlin", firstPerlin)
	total := api.Add(firstPerlin, firstPerlin)

	for i := 1; i < 3; i++ {
		lShift := big.NewInt(1)
		lShift.Lsh(lShift, uint(i))
		perlin, err := SingleScalePerlin(
			api,
			denominator,
			[2]frontend.Variable{xAdjusted, yAdjusted},
			api.Mul(scale, lShift),
			key,
		)
		if err != nil {
			return nil, err
		}
		// api.Println("perlin", perlin)

		total = api.MulAcc(total, perlin, 1)
	}

	totalDividedByCount := api.Div(total, 4)
	// api.Println("totalDividedByCount", totalDividedByCount)

	// totalDividedByCount is between [-DENOMINATOR*sqrt(2)/2, DENOMINATOR*sqrt(2)/2]
	divBy16Quotient, _ := Modulo(
		api,
		api.Mul(totalDividedByCount, 16),
		denominator,
	)
	// api.Println("divBy16Quotient", divBy16Quotient)

	var out = api.Add(divBy16Quotient, 16)
	// api.Println("Perlin:", out)

	return out, nil

}
