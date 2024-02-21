/*
Copyright © 2020 ConsenSys

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

// Fork of gnark MiMC impl which allows configuring a seed & number of rounds.
// (Ideally) meant to be equivalent with the accompanying C# implementation.
// FWIW, I'm a little confused by gnark's MiMC implementation.
// They don't use a key value in the way described [here](https://byt3bit.github.io/primesym/mimc/),
// and also not exactly in the way it's described in the Feistel-MiMC construction.
// Regardless, I'm mirroring its logic as I trust their implementation over one of my own (for now).
package mimcbn254

import (
	"math/big"

	"github.com/consensys/gnark-crypto/ecc"
	"github.com/consensys/gnark-crypto/ecc/bn254/fr"
	"github.com/consensys/gnark/frontend"
	"golang.org/x/crypto/sha3"
)

// MiMC contains the params of the Mimc hash func and the curves on which it is implemented
type MiMC struct {
	params []big.Int           // slice containing constants for the encryption rounds
	id     ecc.ID              // id needed to know which encryption function to use
	h      frontend.Variable   // current vector in the Miyaguchi–Preneel scheme
	data   []frontend.Variable // state storage. data is updated when Write() is called. Sum sums the data.
	api    frontend.API        // underlying constraint system
}

// NewMiMC returns a MiMC instance, than can be used in a gnark circuit
// ASSUMES FRONTEND IS USING BN254 CURVE
func NewMiMC(
	api frontend.API,
	seed string,
	numRounds int,
) (MiMC, error) {
	res := MiMC{}
	res.id = ecc.BN254
	res.h = 0
	res.api = api
	constants, err := initConstants(api, seed, numRounds)
	if err != nil {
		return MiMC{}, err
	}
	res.params = constants

	return res, nil
}

// `seed` doesn't need to be a variable / constrained since it is a
// hardcoded public constant in a smart contract used in verification,
// can similarly be provided to this function as a string
func initConstants(
	api frontend.API,
	seed string,
	numRounds int,
) ([]big.Int, error) {
	bseed := ([]byte)(seed)

	hash := sha3.NewLegacyKeccak256()
	_, _ = hash.Write(bseed)
	rnd := hash.Sum(nil) // pre hash before use
	hash.Reset()
	_, _ = hash.Write(rnd)

	constants := make([]big.Int, numRounds)
	var temp fr.Element
	for i := 0; i < numRounds; i++ {
		rnd = hash.Sum(nil)
		temp.SetBytes(rnd)
		temp.BigInt(&constants[i])
		hash.Reset()
		_, _ = hash.Write(rnd)
	}

	return constants, nil
}

// Write adds more data to the running hash.
func (h *MiMC) Write(data ...frontend.Variable) {
	h.data = append(h.data, data...)
}

// Reset resets the Hash to its initial state.
func (h *MiMC) Reset() {
	h.data = nil
	h.h = 0
}

// Sum hash (in r1cs form) using Miyaguchi–Preneel:
// https://en.wikipedia.org/wiki/One-way_compression_function
// The XOR operation is replaced by field addition.
// See github.com/consensys/gnark-crypto for reference implementation.
func (h *MiMC) Sum() frontend.Variable {

	//h.Write(data...)s
	for _, stream := range h.data {
		r := encryptPow5(*h, stream)
		h.h = h.api.Add(h.h, r, stream)
	}

	h.data = nil // flush the data already hashed

	return h.h

}

// encryptBn256 of a mimc run expressed as r1cs
// m is the message, k the key
func encryptPow5(h MiMC, m frontend.Variable) frontend.Variable {
	x := m
	for i := 0; i < len(h.params); i++ {
		x = pow5(h.api, h.api.Add(x, h.h, h.params[i]))
	}
	return h.api.Add(x, h.h)
}

func pow5(api frontend.API, x frontend.Variable) frontend.Variable {
	r := api.Mul(x, x)
	r = api.Mul(r, r)
	return api.Mul(r, x)
}
