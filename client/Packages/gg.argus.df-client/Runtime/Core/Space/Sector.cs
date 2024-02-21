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

using ArgusLabs.DF.Core.Configs;
using UnityEngine;

namespace ArgusLabs.DF.Core.Space
{
    public struct Sector
    {
        public Vector2Int Position { get; set; }
        public SpaceEnvironment Environment { get; set; }
        public Planet Planet { get; set; }
        
        public bool HasPlanet => Planet is not null;
        public bool WasExplored { get; }
        public int Weight => HasPlanet ? int.MaxValue - 1 - Planet.Level : int.MaxValue;
        
        public Sector(Vector2Int position, SpaceEnvironment environment, Planet planet)
        {
            Position = position;
            Planet = planet;
            Environment = environment;
            
            // Why? This was created via the constructor.
            WasExplored = true;
        }

        public bool TryGetPlanet(out Planet planet)
        {
            planet = Planet;
            
            return HasPlanet;
        }
    }
}