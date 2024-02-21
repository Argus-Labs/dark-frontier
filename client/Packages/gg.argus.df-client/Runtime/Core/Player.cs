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
using ArgusLabs.DF.Core.Space;
using UnityEngine;

namespace ArgusLabs.DF.Core
{
    public class Player
    {
        // I'm making this private because it's too easy to accidentally add a planet directly to the hash without properly claiming it.
        private readonly HashSet<Planet> _planets = new();
        public static Player Alien { get; private set; }
        
        /// <summary>
        /// This is the unique identifier for the player. It's used as the deviceId that Nakama expects and is not the same as the <see cref="PersonaTag"/>.
        /// </summary>
        public string Uid { get; set; }
        public string PersonaTag { get; set; }
        public Color Color { get; set; }
        public Vector2Int HomePosition { get; set; }
        public int Rank { get; set; }
        public int Score { get; set; }
        
        public bool IsLocal { get; private set; }
        public bool IsEnemy => !IsLocal;
        public bool IsAlien => this == Alien;
        
        public int PlanetCount => _planets.Count;

        public Player(string uid = null, bool isLocal = false)
        {
            Uid = uid;
            IsLocal = isLocal;
        }

        public static void InitializeAlien(string uid, string personaTag, Color color)
            => Alien = new Player(uid) { PersonaTag = personaTag, Color = color };
        
        public void Claim(Planet planet)
        {
            // Given the way we currently use this, this is acceptable.
            if (this == planet.Owner)
                return;
            
            planet.Owner = this;
            _ = _planets.Add(planet);
            Debug.Log($"{PersonaTag} claimed: {planet}");
        }
        
        public void Disclaim(Planet planet)
        {
            // Given the way we currently use this, this is acceptable.
            if (this != planet.Owner)
                return;
            
            _ = _planets.Remove(planet);
            Debug.Log($"{PersonaTag} disclaimed: {planet}");

            if (_planets.Count == 0)
                Debug.Log($"{PersonaTag} has no planets left!");
        }
        
        public bool Owns(Planet planet) => planet is not null && _planets.Contains(planet);

        public bool IsHomePlanet(Planet planet) => planet?.Position == HomePosition;
        
        public bool QueryPlanets(IList<Planet> results = null, Func<Planet, bool> predicate = null)
        {
            foreach (var planet in _planets)
                if (predicate is null || predicate.Invoke(planet))
                    results?.Add(planet);

            return results?.Count > 0;
        }
        
        public void ForEachPlanet(Action<Planet> planetCallback)
        {
            foreach (var planet in _planets)
                planetCallback(planet);
        }
    }
}