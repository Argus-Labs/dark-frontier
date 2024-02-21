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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Unity.Mathematics;

namespace ArgusLabs.DF.Core.Miner
{
    public struct GradientAtPoint
    {
        public Vector coords;
        public Vector gradient;
    }

    public struct Vector
    {
        public decimal x;
        public decimal y;

        public Vector(decimal x = 0, decimal y = 0)
        {
            this.x = x;
            this.y = y;
        }

        public Vector ScalarMultiply(decimal scalar)
        {
            return new Vector(this.x * scalar, this.y * scalar);
        }

        public decimal DotProduct(Vector v)
        {
            return this.x * v.x + this.y * v.y;
        }

        public Vector Clone()
        {
            return new Vector(this.x, this.y);
        }
    }

    public class PerlinGenerator
    {
        public const int MAX_PERLIN_VALUE = 32;

        private readonly MimcBN254 _mimcHasher;
        private readonly int _scale;
        private readonly bool2 _willMirror;
        
        private readonly List<Vector> _gradientVectors = new();

        /// <summary>
        /// Initialize Perlin noise generator with the given option
        /// </summary>
        /// <param name="mimcHasher">MimcBN254 hasher used to generate pseudo-random noise values.</param>
        /// <param name="scale">The scale of the noise generator.</param>
        /// <param name="willMirror">Whether the noise generator should mirror the noise.</param>
        public PerlinGenerator(MimcBN254 mimcHasher, int scale, bool2 willMirror)
        {
            _mimcHasher = mimcHasher;
            _scale = scale;
            _willMirror = willMirror;

            // Initialize the gradient vectors
            List<Vector> v = new List<Vector>
        {
            new Vector { x = 1000, y = 0 },
            new Vector { x = 923, y = 382 },
            new Vector { x = 707, y = 707 },
            new Vector { x = 382, y = 923 },
            new Vector { x = 0, y = 1000 },
            new Vector { x = -383, y = 923 },
            new Vector { x = -708, y = 707 },
            new Vector { x = -924, y = 382 },
            new Vector { x = -1000, y = 0 },
            new Vector { x = -924, y = -383 },
            new Vector { x = -708, y = -708 },
            new Vector { x = -383, y = -924 },
            new Vector { x = -1, y = -1000 },
            new Vector { x = 382, y = -924 },
            new Vector { x = 707, y = -708 },
            new Vector { x = 923, y = -383 },
        };

            // iterate through array, divide by 1000, and push to vecs
            foreach (Vector vec in v)
            {
                _gradientVectors.Add(new Vector { x = vec.x / 1000, y = vec.y / 1000 });
            }
        }
        
        // Reusable - This is the main thread.
        private List<ECFieldElement> _writeFields = new();
        
        public int Random(Vector point, int scale)
        {
            _writeFields.Clear();
            _writeFields.Add(MimcBN254.Fp.FromBigInteger(new BigInteger(point.x.ToString()).Mod(MimcBN254.Fp.Q)));
            _writeFields.Add(MimcBN254.Fp.FromBigInteger(new BigInteger(point.y.ToString()).Mod(MimcBN254.Fp.Q)));
            _writeFields.Add(MimcBN254.Fp.FromBigInteger(new BigInteger(scale.ToString())));
            _mimcHasher.Write(_writeFields);
            
            // _mimcHasher.Write(
            //     new List<ECFieldElement>
            //     {
            //         MimcBN254.Fp.FromBigInteger(new BigInteger(point.x.ToString()).Mod(MimcBN254.Fp.Q)),
            //         MimcBN254.Fp.FromBigInteger(new BigInteger(point.y.ToString()).Mod(MimcBN254.Fp.Q)),
            //         MimcBN254.Fp.FromBigInteger(new BigInteger(scale.ToString()))
            //     }
            //);
            var hash = _mimcHasher.Sum();
            _mimcHasher.Reset();

            return hash.ToBigInteger().Mod(new BigInteger("16")).IntValue;
        }
        
        public Vector GetRandomGradientAt(Vector p, int scale)
        {
            int r = this.Random(p, scale);
            return this._gradientVectors[r];
        }

        /// <summary>
        /// Pseudo-random number generator used to select
        /// one of the 16 possible gradient vector
        /// </summary>
        /// <param name="point"></param>
        /// <param name="scale"></param>
        /// <param name="cancellation"></param>
        /// <returns>A random number between 0 and 15</returns>
        public async ValueTask<int> RandomAsync(Vector point, int scale, CancellationToken cancellation = default)
        {
            _writeFields.Clear(); // Still soooo bad. We really should not be doing this.
            _writeFields.Add(MimcBN254.Fp.FromBigInteger(new BigInteger(point.x.ToString()).Mod(MimcBN254.Fp.Q)));
            _writeFields.Add(MimcBN254.Fp.FromBigInteger(new BigInteger(point.y.ToString()).Mod(MimcBN254.Fp.Q)));
            _writeFields.Add(MimcBN254.Fp.FromBigInteger(new BigInteger(scale.ToString())));
            _mimcHasher.Write(_writeFields);
            
            // _mimcHasher.Write(
            //     new List<ECFieldElement>
            //     {
            //     MimcBN254.Fp.FromBigInteger(new BigInteger(point.x.ToString()).Mod(MimcBN254.Fp.Q)),
            //     MimcBN254.Fp.FromBigInteger(new BigInteger(point.y.ToString()).Mod(MimcBN254.Fp.Q)),
            //     MimcBN254.Fp.FromBigInteger(new BigInteger(scale.ToString()))
            //     }
            // );
            var hash = await _mimcHasher.SumAsync(cancellation);
            _mimcHasher.Reset();

            return hash.ToBigInteger().Mod(new BigInteger("16")).IntValue;
        }
        
        public async ValueTask<Vector> GetRandomGradientAtAsync(Vector p, int scale, CancellationToken cancellation = default)
        {
            int r = await this.RandomAsync(p, scale, cancellation);
            return this._gradientVectors[r];
        }

        private decimal GetWeight(Vector corner, Vector p)
        {
            return (1 - Math.Abs(p.x - corner.x)) * (1 - Math.Abs(p.y - corner.y));
        }

        private decimal CalculatePerlinValue(List<GradientAtPoint> corners, decimal scale, Vector point)
        {
            decimal perlinWeights = 0;
            foreach (GradientAtPoint corner in corners)
            {
                Vector distVec = new Vector
                {
                    x = point.x - corner.coords.x,
                    y = point.y - corner.coords.y
                };

                decimal inverseScale = 1m / scale;

                decimal weight =
                    GetWeight(
                        corner.coords.ScalarMultiply(inverseScale),
                        point.ScalarMultiply(inverseScale)
                    ) * (distVec.ScalarMultiply(inverseScale)).DotProduct(corner.gradient);

                perlinWeights += weight;
            }
            
            return perlinWeights;
        }

        private decimal RealMod(decimal dividend, decimal divisor)
        {
            decimal remainder = dividend % divisor;

            // If the remainder is a negative number, add the divisor to it
            if (remainder < 0)
            {
                return remainder + divisor;
            }

            return remainder;
        }
        
        private decimal ValueAt(Vector point, int scale)
        {
            Vector bottomLeftCoords = new Vector
            {
                x = point.x - RealMod(point.x, scale),
                y = point.y - RealMod(point.y, scale)
            };

            Vector bottomRightCoords = new Vector
            {
                x = bottomLeftCoords.x + scale,
                y = bottomLeftCoords.y
            };
            Vector topLeftCoords = new Vector
            {
                x = bottomLeftCoords.x,
                y = bottomLeftCoords.y + scale
            };
            Vector topRightCoords = new Vector
            {
                x = bottomLeftCoords.x + scale,
                y = bottomLeftCoords.y + scale
            };

            GradientAtPoint bottomLeftGrad = new GradientAtPoint
            {
                coords = bottomLeftCoords,
                gradient = this.GetRandomGradientAt(bottomLeftCoords, scale)
            };

            GradientAtPoint bottomRightGrad = new GradientAtPoint
            {
                coords = bottomRightCoords,
                gradient = this.GetRandomGradientAt(bottomRightCoords, scale)
            };

            GradientAtPoint topLeftGrad = new GradientAtPoint
            {
                coords = topLeftCoords,
                gradient = this.GetRandomGradientAt(topLeftCoords, scale)
            };

            GradientAtPoint topRightGrad = new GradientAtPoint
            {
                coords = topRightCoords,
                gradient = this.GetRandomGradientAt(topRightCoords, scale)
            };

            decimal abc = CalculatePerlinValue(
                new List<GradientAtPoint>
                {
                bottomLeftGrad,
                bottomRightGrad,
                topLeftGrad,
                topRightGrad
                },
                scale,
                point
            );

            return abc;
        }

        private async ValueTask<decimal> ValueAtAsync(Vector point, int scale, CancellationToken cancellation = default)
        {
            Vector bottomLeftCoords = new Vector
            {
                x = point.x - RealMod(point.x, scale),
                y = point.y - RealMod(point.y, scale)
            };

            Vector bottomRightCoords = new Vector
            {
                x = bottomLeftCoords.x + scale,
                y = bottomLeftCoords.y
            };
            Vector topLeftCoords = new Vector
            {
                x = bottomLeftCoords.x,
                y = bottomLeftCoords.y + scale
            };
            Vector topRightCoords = new Vector
            {
                x = bottomLeftCoords.x + scale,
                y = bottomLeftCoords.y + scale
            };

            GradientAtPoint bottomLeftGrad = new GradientAtPoint
            {
                coords = bottomLeftCoords,
                gradient = await this.GetRandomGradientAtAsync(bottomLeftCoords, scale, cancellation)
            };

            if (cancellation.IsCancellationRequested)
                return 0;

            GradientAtPoint bottomRightGrad = new GradientAtPoint
            {
                coords = bottomRightCoords,
                gradient = await this.GetRandomGradientAtAsync(bottomRightCoords, scale, cancellation)
            };
            
            if (cancellation.IsCancellationRequested)
                return 0;

            GradientAtPoint topLeftGrad = new GradientAtPoint
            {
                coords = topLeftCoords,
                gradient = await this.GetRandomGradientAtAsync(topLeftCoords, scale, cancellation)
            };
            
            if (cancellation.IsCancellationRequested)
                return 0;

            GradientAtPoint topRightGrad = new GradientAtPoint
            {
                coords = topRightCoords,
                gradient = await this.GetRandomGradientAtAsync(topRightCoords, scale, cancellation)
            };
            
            if (cancellation.IsCancellationRequested)
                return 0;

            decimal abc = CalculatePerlinValue(
                new List<GradientAtPoint>
                {
                bottomLeftGrad,
                bottomRightGrad,
                topLeftGrad,
                topRightGrad
                },
                scale,
                point
            );

            // Console.WriteLine(abc);

            return abc;
        }

        private List<decimal> _pValues = new();
        
        public int PerlinValueAt(Vector point)
        {
            if (_willMirror.x)
            {
                point.x = Math.Abs(point.x);
            }

            if (_willMirror.y)
            {
                point.y = Math.Abs(point.y);
            }

            //List<decimal> pValues = new List<decimal> { };
            _pValues.Clear();
            
            for (int i = 0; i < 3; i++)
            {
                _pValues.Add(ValueAt(point.Clone(), this._scale * Convert.ToInt32(Math.Pow(2, i))));
            }
            
            decimal pVal = (_pValues[0] * 2 + _pValues[1] + _pValues[2]) / 4;
            
            pVal = pVal * (MAX_PERLIN_VALUE / 2);
            pVal = Math.Floor(pVal);
            
            pVal = pVal + (MAX_PERLIN_VALUE / 2);
            
            return Convert.ToInt32(Math.Floor(pVal * 100) / 100);
        }

        private List<decimal> _pValuesForAsync = new();
        
        public async ValueTask<int> PerlinValueAtAsync(Vector point, CancellationToken cancellation = default)
        {
            if (_willMirror.x)
            {
                point.x = Math.Abs(point.x);
            }

            if (_willMirror.y)
            {
                point.y = Math.Abs(point.y);
            }

            //List<decimal> pValues = new List<decimal> { }; // Ugh. Allocating for no reason.
            _pValuesForAsync.Clear();
            
            for (int i = 0; i < 3 && !cancellation.IsCancellationRequested; i++)
                _pValuesForAsync.Add(await ValueAtAsync(point.Clone(), this._scale * Convert.ToInt32(Math.Pow(2, i)), cancellation));
            
            if (cancellation.IsCancellationRequested)
                return 0;
            
            decimal pVal = (_pValuesForAsync[0] * 2 + _pValuesForAsync[1] + _pValuesForAsync[2]) / 4;
            
            pVal = pVal * (MAX_PERLIN_VALUE / 2);
            pVal = Math.Floor(pVal);
            
            pVal = pVal + (MAX_PERLIN_VALUE / 2);
            
            return Convert.ToInt32(Math.Floor(pVal * 100) / 100);
        }
    }
}
