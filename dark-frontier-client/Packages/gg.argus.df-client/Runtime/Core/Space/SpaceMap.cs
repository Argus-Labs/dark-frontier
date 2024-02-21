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
using System.Threading;
using System.Threading.Tasks;
using ArgusLabs.DF.Core.Configs;
using UnityEngine;
using UnityEngine.Assertions;

namespace ArgusLabs.DF.Core.Space
{
    public class SpaceMap
    {
        // Trying this just in case the larger initial allocations alleviate a WebGL build issue.
        // Trying to figure out where it's coming from.
        private const int InitialCollectionCapacity = 1024 * 1024;
        private readonly Dictionary<Vector2Int, Sector> _sectorsByPosition = new(InitialCollectionCapacity);
        private readonly Dictionary<string, Sector> _sectorsByHash = new(InitialCollectionCapacity);
        private readonly SpaceMapper _mapper;
        private readonly GameConfig _config;
        private readonly EventManager _eventManager;
        
        private readonly HashSet<Vector2Int> _positionsWithMatchingNeighbors = new(InitialCollectionCapacity);
        private readonly HashSet<Vector2Int> _knownOrAdjacentPositions = new(InitialCollectionCapacity);
        
        // Max possible for now. WorldRadius may be different.
        private const int AabbScale = 16384;
        private const int AabbHalfScale = AabbScale >> 1;
        public Quadtree Quadtree { get; } = new (new Rect(-AabbHalfScale,-AabbHalfScale, AabbScale, AabbScale));
        
        public long WorldRadius => _config.WorldRadius;
        
        public Sector this[Vector2Int position]
        {
            get
            {
                Sector sector;
                
                if (!IsValidPosition(position))
                {
                    // This will record a planetless DarkSpace (fog) sector and set WasExplored to true.
                    sector = new Sector(position, SpaceEnvironment.DarkSpace, null);
                    this[position] = sector;
                
                    return sector;
                }
                
                _ = _sectorsByPosition.TryGetValue(position, out sector);
                sector.Position = position;
                
                return sector;
            }

            set
            {
                if (!_sectorsByPosition.ContainsKey(position))
                    for (int j = -1; j <= 1; ++j)
                        for (int i = -1; i <= 1; ++i)
                            _ = _knownOrAdjacentPositions.Add(position + new Vector2Int(i, j));
                
                _sectorsByPosition[position] = value;

                if (value.HasPlanet && !string.IsNullOrWhiteSpace(value.Planet.LocationHash))
                    _sectorsByHash[value.Planet.LocationHash] = value;
                
                if (value.HasPlanet)
                    Quadtree.AddSector(value);
                
                _eventManager.GeneralEvents.SectorChanged?.Invoke(value);
            }
        }
        
        public SpaceMap(SpaceMapper mapper, GameConfig config, EventManager eventManager)
        {
            _mapper = mapper;
            _config = config;
            _eventManager = eventManager;
        }
        
        public bool IsValidPosition(Vector2Int position) => position.sqrMagnitude < WorldRadius * WorldRadius;
        
        // Note: No safety.
        public bool TryHashToPlanet(string locationHash, out Planet planet)
        {
            planet = null;
            
            if (!_sectorsByHash.TryGetValue(locationHash, out Sector sector))
                return false;
            
            planet = sector.Planet;
            Assert.IsNotNull(planet, $"{nameof(TryHashToPlanet)}: Planet should never be null. LocationHash: {locationHash}");
            
            return true;
        }
        
        public Sector Explore(Vector2Int position)
        {
            // Note: A default struct will be returned regardless of whether an entry was found. See above.
            Sector sector = this[position];
            
            // Note: WasExplored is set in the constructor.
            if (!sector.WasExplored)
            {
                _ = _mapper.TryMapToPlanet(position, out var planet, out var spaceAreaConfig);
                sector = new Sector(position, spaceAreaConfig.Environment, planet);
                this[position] = sector;
                _eventManager.GeneralEvents.SectorExplored?.Invoke(sector);
            }
            
            return sector;
        }
        
        public async ValueTask<Sector> ExploreAsync(Vector2Int position, CancellationToken cancellation = default)
        {
            // Note: A default struct will be returned regardless of whether an entry was found. See above.
            Sector sector = this[position];
            
            // Note: WasExplored is set in the constructor.
            if (!sector.WasExplored)
            {
                var (planet, spaceAreaConfig, _) = await _mapper.TryMapToPlanetAsync(position, cancellation); //, out var planet, out var spaceAreaAttributes);
                sector = new Sector(position, spaceAreaConfig.Environment, planet);
                this[position] = sector;
                _eventManager.GeneralEvents.SectorExplored?.Invoke(sector);
            }
            
            return sector;
        }
        
        public bool HasExploredNeighbors(Vector2Int position)
            => _knownOrAdjacentPositions.Contains(position);
        
        public bool HasMoreThanOneNeighbor(Vector2Int position, Vector2Int[] neighborOffsets)
        {
            int neighborCount = 0;
                
            foreach (var neighborOffset in neighborOffsets)
                if (this[position + neighborOffset].WasExplored)
                    if (++neighborCount > 1)
                        return true;
                
            return false;
        }
        
        public bool MatchesAllNeighbors(Vector2Int position, SpaceEnvironment environment)
        {
            if (_positionsWithMatchingNeighbors.Contains(position))
                return true;
            
            for (int j = -1; j <= 1; ++j)
            {
                for (int i = -1; i <= 1; ++i)
                {
                    var p = position + new Vector2Int(i, j);
                    var sector = this[p];

                    if (sector.WasExplored || environment == SpaceEnvironment.DarkSpace)
                    {
                        if (environment != SpaceEnvironment.DarkSpace)
                            return false;
                        continue;
                    }
                    
                    if (sector.Environment != environment)
                        return false;
                    
                    if (!sector.WasExplored)
                        return false;
                }
            }
            
            _positionsWithMatchingNeighbors.Add(position);
            
            return true;
        }
    }
}