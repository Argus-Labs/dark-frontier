package perlin

import (
	"math/big"

	"github.com/consensys/gnark/frontend"
)

// Return 1 if a == b, 0 otherwise
func IsEqual(
	api frontend.API,
	a frontend.Variable,
	b frontend.Variable,
) frontend.Variable {
	return api.IsZero(api.Sub(a, b))
}

func QuinSelector(
	api frontend.API,
	index frontend.Variable,
	choices []frontend.Variable,
) frontend.Variable {
	///////////////////////////////////
	// Ensure that index < # choices //
	///////////////////////////////////
	// Circom version uses LessThan(4)
	// => also constrains index, # choices to fit in 4+1 = 5 bits
	numChoices := big.NewInt(int64(len(choices)))
	api.ToBinary(index, 5)
	api.ToBinary(numChoices, 5)
	api.AssertIsLessOrEqual(index, api.Sub(numChoices, 1))

	// Initialize total
	total := api.Mul(
		api.IsZero(index),
		choices[0],
	)

	for i := 1; i < len(choices); i++ {
		total = api.MulAcc(
			total,
			choices[i],
			IsEqual(api, index, i),
		)
	}

	return total
}
