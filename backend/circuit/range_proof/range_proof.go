package range_proof

import (
	"math/big"

	"github.com/consensys/gnark/frontend"
)

type RangeCircuit struct {
	MaxAbsValue frontend.Variable
	In          frontend.Variable
}

func (circuit *RangeCircuit) Define(api frontend.API) error {
	RangeProof(api, 64, circuit.MaxAbsValue, circuit.In)
	return nil
}

// Constrain that 0 <= abs(in) <= maxAbsValue
// =>             0 <= in + maxAbsValue <= 2*maxAbsValue
func RangeProof(
	api frontend.API,
	numBits int,
	maxAbsValue frontend.Variable,
	in frontend.Variable,
) {
	////////////////////////////////////////////////////////////////////////
	// Check that both max and abs(in) are expressible in `numBits` bits  //
	////////////////////////////////////////////////////////////////////////
	lShift := big.NewInt(1)
	lShift.Lsh(lShift, uint(numBits))
	api.ToBinary(api.Add(in, lShift), numBits+1)
	api.ToBinary(maxAbsValue, numBits)

	////////////////////////////////////////////////
	// Check that in + max is between 0 and 2*max //
	////////////////////////////////////////////////
	// Isn't it sufficient to check that in + max <= 2*max?
	// No such thing as "negative", it just becomes a big number (p-x)
	// Anyway, options here are to only check <= 2*max, use IsNegative, or roll my own LessThan circuit
	// I don't like the third option, and I only want to do the second if the first is wrong
	inPlusMax := api.Add(in, maxAbsValue)
	twoMax := api.Mul(maxAbsValue, 2)
	// api.AssertIsLessOrEqual giving me issues here
	// (can't pass in an int literal to first arg),
	// so using api.Cmp
	// https://github.com/ConsenSys/gnark/pull/511 should fix this
	// api.AssertIsDifferent(
	// 	api.Cmp(
	// 		0,
	// 		inPlusMax,
	// 	),
	// 	1,
	// )
	api.AssertIsDifferent(
		api.Cmp(
			inPlusMax,
			twoMax,
		),
		1,
	)
}

// Constrain 0 <= abs(in[i]) <= maxAbsValue
// for all variables in `in`
func MultiRangeProof(
	api frontend.API,
	numBits int,
	maxAbsValue frontend.Variable,
	in ...frontend.Variable,
) {
	for _, v := range in {
		RangeProof(api, numBits, maxAbsValue, v)
	}
}
