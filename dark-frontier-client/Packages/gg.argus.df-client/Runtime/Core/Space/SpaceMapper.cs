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
using System.Threading;
using System.Threading.Tasks;
using ArgusLabs.DF.Core.Configs;
using ArgusLabs.DF.Core.Miner;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using UnityEngine;
using UnityEngine.Pool;
using Assert = UnityEngine.Assertions.Assert;

namespace ArgusLabs.DF.Core.Space
{
    public sealed class SpaceMapper
    {
        private readonly MimcBN254 _mimcLocationHasher;
        private readonly PerlinGenerator _perlinGenerator;
        
        private readonly long[] _noiseThresholds;
        private readonly SpaceAreaConfig[] _spaceAreas;
        private readonly PlanetConfig[] _planetLevels;

        public SpaceMapper(GameConfig config)
        {
            _noiseThresholds = config.SpacePerlinThresholds;
            _spaceAreas = config.SpaceAreas;
            _planetLevels = config.Planets;

            _mimcLocationHasher = new MimcBN254(config.LocationHashSeedWord, config.LocationHashNumRounds);
            var mimcPerlinHasher = new MimcBN254(config.PerlinSeedWord, config.PerlinNumRounds);
            _perlinGenerator = new PerlinGenerator(mimcPerlinHasher, config.Scale, config.WillMirror);
            
            Assert.AreEqual(11, _planetLevels.Length);
            Assert.AreEqual(_noiseThresholds.Length, _spaceAreas.Length - 1,
                "There must be one more space area than there are noise thresholds.");
        }
        
        public int PerlinValueAt(Vector2Int p)
        {
            return _perlinGenerator.PerlinValueAt(new Vector(p.x, p.y));
        }

        // p is grid point before offset
        public async ValueTask<int> PerlinValueAtAsync(Vector2Int p)
        {
            return await _perlinGenerator.PerlinValueAtAsync(new Vector(p.x, p.y));
        }
        
        public string MimcHashValueAt(Vector2Int p)
        {
            var x = new BigInteger(p.x.ToString()).Mod(MimcBN254.Fp.Q);
            var y = new BigInteger(p.y.ToString()).Mod(MimcBN254.Fp.Q);
            var mcx = MimcBN254.Fp.FromBigInteger(x);
            var mcy = MimcBN254.Fp.FromBigInteger(y);
            
            var hashElements = ListPool<ECFieldElement>.Get();
            
            // Concat x and y to get a unique hash for each cell
            hashElements.Add(mcx);
            hashElements.Add(mcy);
            
            // Calculate mimc hash
            _mimcLocationHasher.Write(hashElements);

            ListPool<ECFieldElement>.Release(hashElements);
            
            // We're converting the hash value to hex and padding zeros on the left.
            // This was validated by converting the padded hex results back to BigInteger
            // and verifying that the old and new bigint values matched.
            var sum = _mimcLocationHasher.Sum();
            var result = sum.ToBigInteger().ToString(16).PadLeft(64, '0');
            
            // Reset hasher
            _mimcLocationHasher.Reset();
            
            return result;
        }
        
        // p is grid point before offset
        public async Task<string> MimcHashValueAtAsync(Vector2Int p, CancellationToken cancellation = default)
        {
            var x = new BigInteger(p.x.ToString()).Mod(MimcBN254.Fp.Q);
            var y = new BigInteger(p.y.ToString()).Mod(MimcBN254.Fp.Q);
            var mcx = MimcBN254.Fp.FromBigInteger(x);
            var mcy = MimcBN254.Fp.FromBigInteger(y);
            
            var hashElements = ListPool<ECFieldElement>.Get();
            
            // Concat x and y to get a unique hash for each cell
            hashElements.Add(mcx);
            hashElements.Add(mcy);
            
            // Calculate mimc hash
            _mimcLocationHasher.Write(hashElements);

            ListPool<ECFieldElement>.Release(hashElements);
            
            // We're converting the hash value to hex and padding zeros on the left.
            // This was validated by converting the padded hex results back to BigInteger
            // and verifying that the old and new bigint values matched.
            var sum = await _mimcLocationHasher.SumAsync(cancellation);
            var result = sum.ToBigInteger().ToString(16).PadLeft(64, '0');
            
            // Reset hasher
            _mimcLocationHasher.Reset();
            
            return result;
        }
        
        // Feels a bit hackish, maybe.
        public SpaceAreaConfig GetSpaceAreaConfigByIndex(int index) => _spaceAreas[index];
        
        public SpaceAreaConfig GetSpaceAreaConfig(Vector2Int p)
        {
            Assert.AreEqual(_noiseThresholds.Length, 2);
            Assert.AreEqual(_spaceAreas.Length, 3);
            
            int noiseValue = _perlinGenerator.PerlinValueAt(new Vector(p.x, p.y));
            SpaceAreaConfig spaceArea;

            if (noiseValue < _noiseThresholds[0])
                spaceArea = _spaceAreas[0];
            else if (noiseValue < _noiseThresholds[1])
                spaceArea = _spaceAreas[1];
            else
                spaceArea = _spaceAreas[2];
            
            return spaceArea;
        }
        
        public async Task<(SpaceAreaConfig, int perlinNoiseValue)> GetSpaceAreaConfigAsync(Vector2Int p, CancellationToken cancellation = default)
        {
            Assert.AreEqual(_noiseThresholds.Length, 2);
            Assert.AreEqual(_spaceAreas.Length, 3);
            
            int noiseValue = await _perlinGenerator.PerlinValueAtAsync(new Vector(p.x, p.y), cancellation);
            SpaceAreaConfig spaceArea;

            if (noiseValue < _noiseThresholds[0])
                spaceArea = _spaceAreas[0];
            else if (noiseValue < _noiseThresholds[1])
                spaceArea = _spaceAreas[1];
            else
                spaceArea = _spaceAreas[2];
            
            return (spaceArea, noiseValue);
        }

        public void RebuildSector(string locationHash, int perlin, ref Sector sector)
        {
            const int hashOffset = 2;
            float outerNoise = (float)Convert.ToUInt16(locationHash.Substring(hashOffset, 4), 16) / UInt16.MaxValue;
            float innerNoise = (float)Convert.ToByte(locationHash.Substring(hashOffset + 4, 2), 16) / byte.MaxValue;
            SpaceAreaConfig spaceArea = _spaceAreas[(int)sector.Environment];
            
            if (outerNoise <= spaceArea.PlanetSpawnThreshold)
            {
                // Compare innerNoise against all planet levels thresholds with new dice roll to determine what level to spawn in cell.
                for (int i = 0; i < spaceArea.PlanetLevelThresholds.Length; ++i)
                {
                    float spawningThreshold = spaceArea.PlanetLevelThresholds[i];
                    
                    if (innerNoise <= spawningThreshold)
                    {
                        var planetLevel = _planetLevels[i];
                        
                        sector.Planet = new Planet(planetLevel, spaceArea)
                        {
                            Id = locationHash,
                            Position = sector.Position,
                            LocationHash = locationHash,
                            PerlinValue = perlin
                        };

                        _ = sector.Planet.TrySetEnergyLevel(planetLevel.EnergyDefault, 0, DateTime.MinValue);
                        
                        break;
                    }
                }
            }
        }
        
        public bool TryMapToPlanet(Vector2Int p, out Planet planet, out SpaceAreaConfig spaceArea)
        {
            bool result = false;
            
            string mimcHash = MimcHashValueAt(p);
            
            const int hashOffset = 2;
            float outerNoise = (float)Convert.ToUInt16(mimcHash.Substring(hashOffset, 4), 16) / UInt16.MaxValue;
            float innerNoise = (float)Convert.ToByte(mimcHash.Substring(hashOffset + 4, 2), 16) / byte.MaxValue;
            
            spaceArea = GetSpaceAreaConfig(p);
            planet = null;
            
            // Compare outerNoise against the chance that any planet can spawn at all.
            if (outerNoise <= spaceArea.PlanetSpawnThreshold)
            {
                // Compare innerNoise against all planet levels thresholds with new dice roll to determine what level to spawn in cell.
                for (int i = 0; i < spaceArea.PlanetLevelThresholds.Length; ++i)
                {
                    float spawningThreshold = spaceArea.PlanetLevelThresholds[i];
                    
                    if (innerNoise <= spawningThreshold)
                    {
                        var planetLevel = _planetLevels[i];
                        
                        planet = new Planet(planetLevel, spaceArea)
                        {
                            Id = mimcHash,
                            Position = p,
                            LocationHash = mimcHash,
                            PerlinValue = PerlinValueAt(p)
                        };

                        _ = planet.TrySetEnergyLevel(planetLevel.EnergyDefault, 0, DateTime.MinValue);
                        
                        result = true;
                        
                        break;
                    }
                }
            }

            return result;
        }

        // Or celestial body?
        public async ValueTask<(Planet, SpaceAreaConfig, bool)> TryMapToPlanetAsync(Vector2Int p, CancellationToken cancellation = default)
        {
            bool result = false;

            Planet planet = null;

            string locationHash = await MimcHashValueAtAsync(p, cancellation);
            
            const int hashOffset = 2;
            float outerNoise = (float)Convert.ToUInt16(locationHash.Substring(hashOffset, 4), 16) / UInt16.MaxValue;
            float innerNoise = (float)Convert.ToByte(locationHash.Substring(hashOffset + 4, 2), 16) / byte.MaxValue;
            
            var (spaceArea, perlinNoiseValue) = await GetSpaceAreaConfigAsync(p, cancellation);
            
            // Compare outerNoise against the chance that any planet can spawn at all.
            if (outerNoise <= spaceArea.PlanetSpawnThreshold)
            {
                // Compare innerNoise against all planet levels thresholds with new dice roll to determine what level to spawn in cell.
                for (int i = 0; i < spaceArea.PlanetLevelThresholds.Length; ++i)
                {
                    float spawningThreshold = spaceArea.PlanetLevelThresholds[i];
                    
                    if (innerNoise <= spawningThreshold)
                    {
                        var planetLevel = _planetLevels[i];
                        
                        planet = new Planet(planetLevel, spaceArea)
                        {
                            Id = locationHash,
                            Position = p,
                            LocationHash = locationHash,
                            PerlinValue = perlinNoiseValue
                        };

                        _ = planet.TrySetEnergyLevel(planetLevel.EnergyDefault, 0, DateTime.MinValue);
                        
                        result = true;
                        
                        break;
                    }
                }
            }
            
            return (planet, spaceArea, result);
        }
    }
}