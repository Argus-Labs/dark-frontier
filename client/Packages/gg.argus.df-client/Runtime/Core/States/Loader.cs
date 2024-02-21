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
using System.Linq;
using System.Threading.Tasks;
using ArgusLabs.DF.Core.Communications;
using ArgusLabs.DF.Core.Configs;
using ArgusLabs.DF.Core.Space;
using Smonch.CyclopsFramework;
using UnityEngine;
using Random = UnityEngine.Random;
using static Unity.Mathematics.math;

namespace ArgusLabs.DF.Core.States
{
    public class Loader : CyclopsGameState
    {
        private readonly GameConfig _config;
        private readonly GameStorageManager _storageManager;
        private readonly EventManager _eventManager;
        private readonly Player _player;
        private readonly SpaceMapper _mapper;
        private readonly SpaceMap _map;
        private readonly bool _forceSearchForHomePlanet;
        
        public List<Sector> History { get; } = new(1024 * 1024);
        
        public bool IsComplete { get; private set; }

        public Loader(
            GameConfig config,
            GameStorageManager storageManager,
            EventManager eventManager,
            Player player,
            SpaceMapper mapper,
            SpaceMap map)
        {
            _config = config;
            _storageManager = storageManager;
            _eventManager = eventManager;
            _player = player;
            _mapper = mapper;
            _map = map;
        }
        
        // CAUTION: REALLY IMPORTANT: Async is ONLY ok here because this state doesn't contain an update loop that could interfere or cause a single-threaded async race condition.
        // It's also ok because the async work is complete before Engine.NextFrame.Loop(stateMachine.Update) is even added.
        protected override void OnEnter()
        {
            Debug.Log($"{nameof(Loader)} {nameof(OnEnter)}");
            
            bool isLookingForHomeWorld = true;
            long a = Mathf.Abs(DateTime.UtcNow.GetHashCode());
            long b = Mathf.Abs(_player.Uid.GetHashCode());
            int seed = (int)((a + b) % int.MaxValue);
            
            Random.InitState(seed);
            
            // TODO: Improve. Operating directly on PlayersPrefs is a bad idea.

            int revision = 0;
            string uid = _player.Uid;
            string homeXKey = $"home.x.2023_8_29_{revision}.{uid}";
            string homeYKey = $"home.y.2023_8_29_{revision}.{uid}";

            if (PlayerPrefs.HasKey(homeXKey) && PlayerPrefs.HasKey(homeYKey))
            {
                int x = PlayerPrefs.GetInt(homeXKey);
                int y = PlayerPrefs.GetInt(homeYKey);

                _player.HomePosition = new Vector2Int(x, y);
                isLookingForHomeWorld = false;
                
                // Record what's there.
                var homeSector = _map.Explore(_player.HomePosition);
                
                if (!_storageManager.WillSkipHomePlanetSearch && !homeSector.HasPlanet)
                    Debug.LogError("Home sector has no planet. Please clear your history.");

                var existingHomePlanet = homeSector.Planet;
                
                _player.Claim(existingHomePlanet);
                
                Debug.Log($"{nameof(_player.HomePosition)} loaded from PlayerPrefs: {_player.HomePosition}");
            }

            _eventManager.GeneralEvents.LoadingStarted?.Invoke(isLookingForHomeWorld);

            // Search for a home planet.
            
            float maxSearchRadius = _config.WorldRadius - 2;
            float minSearchRadius = _config.WorldRadius * (2f / 3f);
            
            Debug.Log($"minSearchRadius: {minSearchRadius} maxSearchRadius: {maxSearchRadius}");
            
            Vector2Int searchPosition = Vector2Int.zero;
            Planet planet = null;
            SpaceAreaConfig spaceAreaConfig = null;
            bool isPlanetApproved = false;

            Debug.Log("Setting up homePlanetSearch and other sub-states.");
            
            // Setup states.
            
            var homePlanetSearch = new CyclopsState
            {
                Entered = () =>
                {
                    Debug.Log("Entered Loader sub-state: homePlanetSearch");
                    Debug.Log("Searching for a suitable home world.");
                },
                Updating = () =>
                {
                    const int maxAttempts = 52;
                    int attemptCount = 0;
                    
                    while (attemptCount < maxAttempts)
                    {
                        float r = Random.Range(minSearchRadius, maxSearchRadius);
                        float a = Random.value * 2f * PI;
                        var sp = new Vector2(r * cos(a), r * sin(a));
                        
                        searchPosition = new Vector2Int((int)sp.x, (int)sp.y);
                        
                        bool TryFindPlanet()
                        {
                            ++attemptCount;
                            
                            if (_mapper.TryMapToPlanet(searchPosition, out planet, out spaceAreaConfig))
                            {
                                Debug.Log($"Investigating: {searchPosition} in {spaceAreaConfig.Environment.ToString()} Space. (Area Index: {(int)spaceAreaConfig.Environment})");
                            
                                if ((spaceAreaConfig.Environment != SpaceEnvironment.Nebula) || (planet.Level != 0))
                                    planet = null;
                
                                if (planet is not null)
                                    return true;
                            }

                            return false;
                        }
                        
                        if (TryFindPlanet())
                            return;

                        if (spaceAreaConfig.Environment == SpaceEnvironment.Nebula)
                        {
                            var searchRect = new RectInt(searchPosition - 2 * Vector2Int.one, 5 * Vector2Int.one);
                            Debug.Log($"Searching nebula in grid pattern. {searchRect.ToString()}");
                            
                            foreach (Vector2Int p in searchRect.allPositionsWithin)
                            {
                                searchPosition = p;
                                
                                if (TryFindPlanet() || (attemptCount > maxAttempts))
                                    return;

                                if (spaceAreaConfig.Environment != SpaceEnvironment.Nebula)
                                    break;
                            }
                        }

                        if (planet is not null)
                            return;
                    }
                
                    // if (_willClusterHomePlanets)
                    //     searchRadius += 3;
                },
                Exited = () => Debug.Log("Exited Loader sub-state: homePlanetSearch")
            };
            
            // Wait to see if that planet is approved by the backend.
            
            void OnHomePlanetClaimApproved()
            {
                if (planet is null)
                {
                    Debug.LogError("OnHomePlanetClaimApproved: The planet should not be null, but it is. Why?");
                    return;
                }
                
                Debug.Log("This looks like home. I'll put the couch over there.");
                Debug.Log(planet.ToString());
                
                _player.HomePosition = searchPosition;
                _map[searchPosition] = new Sector(searchPosition, spaceAreaConfig.Environment, planet);
                _player.Claim(planet);
                
                // TODO: Improve. Operating directly on PlayersPrefs is a bad idea.
                PlayerPrefs.SetInt(homeXKey, searchPosition.x);
                PlayerPrefs.SetInt(homeYKey, searchPosition.y);
                        
                isPlanetApproved = true;
                Debug.Log("Approved.");
            }

            void OnHomePlanetClaimDenied()
            {
                Debug.Log("OnHomePlanetClaimDenied");

                if (_storageManager.WillSkipHomePlanetSearch)
                {
                    Debug.Log("Disregard. Thanks to hacking the planet, we're going to assign this one to you. It may not seem like home at first, and it may not even be yours, but we think you'll appreciate it in due time.");
                    OnHomePlanetClaimApproved();

                    return;
                }
                
                planet = null;
            }

            var homePlanetValidation = new CyclopsState
            {
                Entered = () =>
                {
                    Debug.Log($"Entered Loader sub-state: homePlanetValidation");
                    _eventManager.GameplayEvents.HomePlanetClaimApproved += OnHomePlanetClaimApproved;
                    _eventManager.GameplayEvents.HomePlanetClaimDenied += OnHomePlanetClaimDenied;
                    
                    _eventManager.GameplayEvents.HomePlanetClaimRequested?.Invoke(
                        searchPosition,
                        new ClaimHomePlanetMsg
                        {
                            perlin = planet.PerlinValue,
                            locationHash = planet.LocationHash
                        });
                },
                Exited = () =>
                {
                    _eventManager.GameplayEvents.HomePlanetClaimApproved -= OnHomePlanetClaimApproved;
                    _eventManager.GameplayEvents.HomePlanetClaimDenied -= OnHomePlanetClaimDenied;
                    Debug.Log($"Exited Loader sub-state: homePlanetValidation");
                }
            };
            
            var historyReplay = new CyclopsState
            {
                // ReSharper disable once AsyncVoidLambda - This is fine. Note: Handles cancellation internally.
                Entered = async () =>
                {
                    if (_storageManager.HasHistory)
                        await ProcessHistoryAsync();
                    else
                        Debug.Log("History does not exist. This is fine unless it should exist.");
                    
                    // We're completely done. Let's move on to the next major game state.
                    IsComplete = true;
                }
            };
            
            homePlanetSearch.AddTransition(
                target: homePlanetValidation,
                predicate: () => planet is not null && !isPlanetApproved);
            
            homePlanetValidation.AddTransition(
                target: homePlanetSearch,
                predicate: () => planet is null);
            
            homePlanetValidation.AddTransition(
                target: historyReplay,
                predicate: () => isPlanetApproved);
            
            var stateMachine = new CyclopsStateMachine();
            
            stateMachine.PushState(isLookingForHomeWorld ? homePlanetSearch : historyReplay);
            Engine.NextFrame.Loop(stateMachine.Update);
        }

        protected override void OnExit()
        {
            _eventManager.GeneralEvents.LoadingFinished?.Invoke();
            Debug.Log($"{nameof(Loader)} {nameof(OnExit)}");
        }

        private async ValueTask ProcessHistoryAsync()
        {
            var planets = new HashSet<Planet>();

            await _storageManager.ProcessExistingHistory(reader =>
            {
                var p = new Vector2Int(reader.ReadInt32(), reader.ReadInt32());
                SpaceEnvironment environment = (SpaceEnvironment)reader.ReadInt32();
                bool hasPlanet = reader.ReadBoolean();
                var sector = new Sector(p, environment, null);
                
                if (hasPlanet)
                {
                    string locationHash = reader.ReadString();
                    int perlin = reader.ReadInt32();
                    _mapper.RebuildSector(locationHash, perlin, ref sector);
                }
                
                _map[p] = sector;
                _ = _storageManager.TryWriteToHistory(sector);
                History.Add(sector);
                
                if (hasPlanet)
                    _ = planets.Add(sector.Planet);
            });
            
            Debug.Log($"{nameof(ProcessHistoryAsync)}: Requesting known planets from backend.");
            
            // The allocation is fine because this is a one-time operation.
            _eventManager.GameplayEvents.PlanetsUpdateRequested?.Invoke(planets.ToArray());
        }
    }
}