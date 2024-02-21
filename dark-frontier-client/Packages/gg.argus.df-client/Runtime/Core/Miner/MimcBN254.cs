// Copyright 2024 Argus Labs
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Math.EC;
using UnityEngine;
using BigInteger = Org.BouncyCastle.Math.BigInteger;

namespace ArgusLabs.DF.Core.Miner
{
    public class MimcBN254
    {
        // NOTE: THIS IS NOT THE BN254 CURVE!!
        // We really just need the curve object to be able to generate `Org.BouncyCastle.Math.EC.FpFieldElement`s from it,
        // so here we define a dummy curve which is *defined over a field with the same order as the group formed by BN254*
        // (namely, the BabyJubJub prime)
        public static FpCurve Fp = new FpCurve(
            new BigInteger("21888242871839275222246405745257275088548364400416034343698204186575808495617"),
            BigInteger.Zero,
            BigInteger.Zero,
            BigInteger.Zero,
            BigInteger.Zero
        );

        // The initial hash state (0 in the field)
        private static ECFieldElement s_initialHashState = MimcBN254.Fp.FromBigInteger(BigInteger.Zero);

        // The number of rounds to run the hash with
        // For our field, this *should* be 110 (log5(BabyJubJub)) according to the paper,
        // but we allow it to be configured
        private int _numRounds;

        // The round constants to be used in each round of the hash
        // Generated pseudo-randomly using Keccak256 from a seed string
        private readonly ECFieldElement[] _roundConstants;

        // The current hash state
        private ECFieldElement _hashState;

        // The data (other field elements) to hash
        private List<ECFieldElement> _input;

        // Creates a new instance of MiMCHash using the provided seed string for
        // pseudo-random round constant generation, and with the provided number of rounds
        public MimcBN254(string seed = "seed", int numRounds = 110)
        {
            _input = new List<ECFieldElement>();
            _hashState = s_initialHashState;

            _numRounds = numRounds;
            _roundConstants = new ECFieldElement[_numRounds];

            // Generate round constants by repeatedly hashing the seed string
            // and parsing the result as a field element
            var digest = new KeccakDigest(256);
            // We use UTF-8 encoding because this is the encoding used in gnark
            var bseed = Encoding.UTF8.GetBytes(seed);
            digest.BlockUpdate(bseed, 0, bseed.Length);
            var rnd = new byte[digest.GetDigestSize()];
            // Calculate a pre-hash before recording round constants
            // Why? Because gnark does it
            digest.DoFinal(rnd, 0);
            digest.Reset();
            digest.BlockUpdate(rnd, 0, rnd.Length);

            for (var i = 0; i < _numRounds; i++)
            {
                // Calculate the next Keccak hash
                digest.DoFinal(rnd, 0);
                // Parse the hash bytes as big-endian unsigned integer
                // (gnark expects big-endianness) & translate into field element
                var roundConstant = Fp.FromBigInteger(
                    new BigInteger(1, rnd).Mod(Fp.Q)
                );
                _roundConstants[i] = roundConstant;
                digest.Reset();
                digest.BlockUpdate(rnd, 0, rnd.Length);
            }
        }

        // Adds data (field elements) to the hash input
        public void Write(List<ECFieldElement> data)
        {
            var newInput = new List<ECFieldElement>(this._input.Count + data.Count);
            newInput.AddRange(this._input);
            newInput.AddRange(data);
            _input = newInput;
        }

        // Reset the hash, i.e. clear the input and set the hash state back to 0
        // This should be called after every time a hash is calculated!
        public void Reset()
        {
            _input.Clear();
            _hashState = MimcBN254.s_initialHashState;
        }

        /*
        Calculates MiMCHash-n/n using Miyaguchi-Preneel one-way compression scheme.
        Not quite the MiMCHash construction specified in the [paper](https://eprint.iacr.org/2016/492.pdf),
        but matches the implementation in [gnark](https://github.com/ConsenSys/gnark/blob/master/std/hash/mimc/mimc.go),
        which itself matches [this implementation](https://gist.github.com/HarryR/80b5ff2ce13da12edafda6d21c780730) from
        the [ethsnarks](https://github.com/HarryR/ethsnarks) team.

        Note that this differs from the [circomlibjs implementation](https://github.com/iden3/circomlibjs/blob/main/src/mimc7.js),
        which uses the Merkle-Damgard compression scheme. Discussion around this can be found [here](https://github.com/HarryR/ethsnarks/issues/119).

        This is all despite the fact that the authors of the MiMC paper softly
        [advise against using compression schemes](https://mimc.iaik.tugraz.at/pages/faq.php), but doing so lets us keep everything in the same field.
        */
        public ECFieldElement Sum()
        {
            foreach (ECFieldElement e in _input)
            {
                var r = EncryptPow5(e);
                r = r.Add(e);
                _hashState = _hashState.Add(r);
            }
            
            _input.Clear();
            
            return _hashState;
        }
        
        // Computes one run of the MiMC-n/n block cipher
        private ECFieldElement EncryptPow5(ECFieldElement e)
        {
            var x = e;
            
            foreach (ECFieldElement roundConstant in _roundConstants)
            {
                x = x.Add(_hashState).Add(roundConstant);
                x = x.Square().Square().Multiply(x);
            }
            
            return x.Add(_hashState);
        }
        
        public async Task<ECFieldElement> SumAsync(CancellationToken cancellation = default)
        {
            foreach (ECFieldElement e in _input)
            {
                if (cancellation.IsCancellationRequested)
                    break;
                
                var r = await EncryptPow5Async(e, cancellation);
                r = r.Add(e);
                _hashState = _hashState.Add(r);
            }
            
            _input.Clear();
            
            return _hashState;
        }
        
        private long _accRoundsCount = 0;
        private float _smoothMaxRoundsPerFrame = 72;
        private readonly FrameTiming[] _frameTimings = new FrameTiming[1];
        
        // Computes one run of the MiMC-n/n block cipher
        private async Task<ECFieldElement> EncryptPow5Async(ECFieldElement e, CancellationToken cancellation = default)
        {
            ECFieldElement x = e;
            FrameTimingManager.GetLatestTimings(1, _frameTimings);
            FrameTiming frameTiming = _frameTimings[0];
            
            // This may allow us to regain some lost performance while waiting for a frame.
            // 4.32 is 72.0 / 16.66666666 repeating.
            // The idea is that if we're targeting 72 @ 60fps, then this should be proportional up to the cap at 144.
            int maxRoundsPerFrame = Mathf.Min(144, 72 + (int)(frameTiming.cpuMainThreadPresentWaitTime * 4.32));
            _smoothMaxRoundsPerFrame = Mathf.Lerp(_smoothMaxRoundsPerFrame, maxRoundsPerFrame, 1f / 15f);
            maxRoundsPerFrame = (int)_smoothMaxRoundsPerFrame;
            
            for (int i = 0; i < _roundConstants.Length && !cancellation.IsCancellationRequested; ++i)
            {
                ECFieldElement roundConstant = _roundConstants[i];
                x = x.Add(_hashState).Add(roundConstant);
                x = x.Square().Square().Multiply(x);
                
                if (++_accRoundsCount >= maxRoundsPerFrame)
                {
                    _accRoundsCount = 0;
                    await Task.Yield();
                }
            }

            return x.Add(_hashState);
        }
    }
}