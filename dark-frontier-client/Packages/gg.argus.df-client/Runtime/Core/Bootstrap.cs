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
using ArgusLabs.WorldEngineClient.Communications;
using ArgusLabs.DF.Core.Communications.Prover;
using ArgusLabs.DF.Core.Configs;
using ArgusLabs.DF.Core.Space;
using ArgusLabs.DF.Core.States;
using ArgusLabs.DF.Input;
using ArgusLabs.DF.UI;
using ArgusLabs.WorldEngineClient.Communications.Prover;
using ArgusLabs.WorldEngineClient.Communications.Rpc;
using Nakama;
using Smonch.CyclopsFramework;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using Cursor = UnityEngine.Cursor;

namespace ArgusLabs.DF.Core
{
    public sealed class Bootstrap : MonoBehaviour
    {
        // While we have no need for a Singleton, it's still a good idea to check for duplicated behaviours.
        private static bool s_wasCreated;
        
        [Header("Communications")]
        
        [SerializeField] private string _clientProtocol = "http";
        [SerializeField] private string _clientHost = "localhost";
        [SerializeField] private ushort _clientPort = 7350;
        [SerializeField] private string _clientServerKey = "defaultkey";
        [SerializeField] private string _cloudProverUri = "http://your-uri-here/generate-proof";
        
        [Header("General")]
        
        [SerializeField] private bool _willStartInCleanTestMode;
        [SerializeField] private bool _willRequireBetaKey;
        [SerializeField] private bool _willRespectCountdown;
        [SerializeField] private int _perlinScaleOverride;
        [SerializeField] private int _worldMaxRadiusOverride;

        [Header("Assets")]
        
        [SerializeField] private GameObject _spaceMapQuadPrefab;
        [SerializeField] private GameObject _ringPrefab;
        [SerializeField] private GameObject _ringSegmentPrefab;
        [SerializeField] private GameObject _squarePrefab;
        [SerializeField] private GameObject _linePrefab;
        [SerializeField] private Font _overlayDefaultGuiFont;
        [SerializeField] private RootUIManager _rootUI;
        [SerializeField] private GameObject _spaceViewPrefab;
        [SerializeField] private Material _spaceMapMaterial;
        [SerializeField] private Material _texelMaterial;
        [SerializeField] private Material _textureCopyMaterial;
        [SerializeField] private MeshAssetsConfig _transportShipConfig;
        [SerializeField] private PlanetAssetsConfig[] _planetAssetConfigs;
        [SerializeField] private GameplayConfig _gameplayConfig;
        
        private CyclopsGame _game;
        private GameConfig _gameConfig;
        private CommunicationsManager _communicationsManager;
        private GameStorageManager _storageManager;
        private EvmAddressStorageManager _linkedEvmAddressStorageManager;
        private OverlayController _overlayController;
        private EventManager _eventManager;
        private Player _player;
        private CloudProver _cloudProver;
        private RenderTexture _spaceMapRT;
        private readonly int _spaceMapId = Shader.PropertyToID("_SpaceMap");

        private bool _personaTagWasAltered;
        private bool _betaKeyWasAltered;
        private bool _playerUidWasAltered;

        // Important: States should only know about dependencies on an as needed basis.
        
        private async void Awake()
        {
            Assert.IsFalse(s_wasCreated, "Only one instance of Bootstrap is allowed.");
            Assert.IsNotNull(Camera.main);
            
            // Important if and when multiple scenes are needed. Currently, they're not.
            DontDestroyOnLoad(gameObject);
            
            // Uncap framerate. It still may be capped by the browser or v-sync settings.
            Application.targetFrameRate = -1;
            
            // Instantiate the game which is responsible for state management.
            _game = new CyclopsGame();
            
            // We're using an event manager to keep things decoupled.
            // There are a few places where we overly complicated things
            // or leaned on the event manager in a less than ideal way.
            // The situation could be improved, but it works for now.
            _eventManager = new EventManager();
            
            // Ensure that player data isn't wiped every time a built game loads.
            if ((Application.platform == RuntimePlatform.WebGLPlayer) && !Application.isEditor)
                _willStartInCleanTestMode = false;
            
            // Initialization is cheap, so no worries.
            _storageManager = new GameStorageManager(willClearAllData: _willStartInCleanTestMode);
            _linkedEvmAddressStorageManager = new EvmAddressStorageManager();
            
            if (!_willStartInCleanTestMode)
                _ = _linkedEvmAddressStorageManager.TryLoadEvmAddresses();
            
            if (_storageManager.PlayerHasClaimedPersonaTag && !_storageManager.PlayerHasValidPersonaTag)
                Debug.LogError("Player's existing persona tag is invalid.");
            
            bool playerHasClaimedPersonaTag = _storageManager.PlayerHasClaimedPersonaTag && _storageManager.PlayerHasValidPersonaTag;
            
            if (playerHasClaimedPersonaTag)
                _storageManager.LogStoredPersonaTag();
            else
                Debug.Log("Player has not claimed persona tag.");
            
            if (_storageManager.TryLoadOrStorePlayerUid(() => Guid.NewGuid().ToString(), out string playerUid))
            {
                Debug.Log($"Player UID: {playerUid}");
            }
            else
            {
                Panic("UID is invalid.");
                return;
            }

            _player = new Player(playerUid, isLocal: true) { Color = ColorPalette.LocalPlayer };
            
            Player.InitializeAlien(string.Empty, "Alien", ColorPalette.UnclaimedGrey);
            
            _overlayController = new OverlayController(Camera.main, _squarePrefab, _linePrefab, _ringPrefab, _ringSegmentPrefab)
            {
                GuiFont = _overlayDefaultGuiFont
            };
            
            var inputActions = new DfInputActions();
            
            // Instantiate a Nakama client for Unity. See: https://github.com/heroiclabs/nakama-unity#unity-webgl
            var client = new Client(_clientProtocol, _clientHost, _clientPort, _clientServerKey, UnityWebRequestAdapter.Instance);
            
            // Handles connections, sessions, matches, RPCs, transaction receipts, etc.
            _communicationsManager = new CommunicationsManager(client) { DefaultCancellationToken = Application.exitCancellationToken };
            
            // We setup and handle our communications events ahead of time so that dependencies are never an issue.
            InitializeOnlineEvents();
            
            // AutoSignInAsync authenticates with a "device id" which in practice is _player.Uid.
            // Following auth, a match is joined. We DO NOT claim or show persona here.
            bool wasSignInSuccessful = await _communicationsManager.AutoSignInAsync(_player.Uid, Application.exitCancellationToken);

            if (!wasSignInSuccessful)
            {
                Panic("Sign in failed.");
                return;
            }
            
            (bool wasSuccessful, GameConfig config) = await _communicationsManager.TryLoadConfigAsync(Application.exitCancellationToken);
            _gameConfig = config;
            
            if (!wasSuccessful)
            {
                Panic("Couldn't load constant configs.");
                return;
            }
            
            if (_perlinScaleOverride != 0)
                Debug.LogWarning($"Perlin Scale Override: {_perlinScaleOverride}");
            
            if (_worldMaxRadiusOverride != 0)
                Debug.LogWarning($"Perlin Scale Override: {_worldMaxRadiusOverride}");
            
            _gameConfig.PerlinScaleOverride = _perlinScaleOverride;
            _gameConfig.RadiusMaxOverride = _worldMaxRadiusOverride;
            
            //show persona if player has claimed beta key, there can be a case where player hasn't claimed persona in local storage but it's claimed in backend so we only check for beta key here
            if (_storageManager.PlayerHasClaimedBetaKey && !_willStartInCleanTestMode)
            {
                // Try 4 times. Create a new player on the 4th. It would be better to have a backend endpoint to check for this.
                for (int i = 0; i < 4; ++i)
                {
                    (RpcResult rpcResult, ShowPersonaMsg msg) = await _communicationsManager.TryShowPersonaAsync(_player.PersonaTag, Application.exitCancellationToken);
                    
                    if (rpcResult.WasSuccessful)
                    {
                        if (msg.Status == ResponseStatus.Unknown)
                        {
                            Debug.LogError("ShowPersonaMsg response status is unknown.");
                            return;
                        }

                        if (msg.Status == ResponseStatus.Pending)
                        {
                            Debug.LogWarning("ShowPersonaMsg response status is pending. Wait and retry");

                            // Wait a bit before we try again.
                            for (float t = 0f; (t < 2f) && !Application.exitCancellationToken.IsCancellationRequested; t += Time.deltaTime)
                                await Task.Yield();
                        }
                        else if (msg.Status == ResponseStatus.Accepted)
                        {
                            Debug.Log("ShowPersonaMsg response status is accepted.");
                            _player.PersonaTag = msg.PersonaTag;
                            playerHasClaimedPersonaTag = true;
                            
                            break;
                        }
                        else if (msg.Status == ResponseStatus.Rejected)
                        {
                            Debug.LogWarning("ShowPersonaMsg response status is rejected. Player will enter setup.");
                            playerHasClaimedPersonaTag = false;
                            
                            break;
                        }
                    }
                    else
                    {
                        if (i < 3)
                            Debug.Log($"ShowPersonaMsg response was not successful ({msg.Status.ToString()}). Retrying.");
                        else 
                            Debug.Log($"ShowPersonaMsg response was not successful ({msg.Status.ToString()}). Max retry attempt reached.");
                    }

                    //player have beta key and persona in local storage but failed show-persona, most likely the backend is reset so we remake the account
                    if (i == 3 && !_storageManager.IsFirstTimePlayer) 
                    {
                        Debug.LogError("Either the backend was reset or we're new to this. We'll start from scratch.");
                        _storageManager.Dispose();
                        _storageManager = new GameStorageManager(willClearAllData: true);
                        _storageManager.TryLoadOrStorePlayerUid(() => playerUid, out _);
                        _linkedEvmAddressStorageManager = new EvmAddressStorageManager();
                        playerHasClaimedPersonaTag = false;

                        break;
                    }
                }
            }

            // This will lag just behind the backend. That should be fine.
            WorldClock worldClock = new(_gameConfig.TicksPerSecond);
            
            worldClock.Resyncing += async () =>
            {
                // Sync to current tick.
                RpcResult tickResult = await _communicationsManager.ReadCurrentTickAsync(Application.exitCancellationToken);
                ulong tick = JsonUtility.FromJson<CurrentTickMsg>(tickResult.Payload).currentTick;

                worldClock.SyncToWorldEngine(tick);

                Debug.Log($"Payload: {tickResult.Payload}");
                Debug.Log($"Deserialized tick: {tick}");
                Debug.Log($"worldClock.CurrentTick: {worldClock.CurrentTick}");
            };
            
            // Heads up: This is non-deterministic. It should be more than fine and we need it yesterday.
            worldClock.Resync();
            
            GameInstanceInfo instanceInfo = new GameInstanceInfo(_gameConfig.InstanceName, _gameConfig.InstanceTimer);
            
            // Pre-instantiate root UI (that contains all of the game's UI) and initialize it with reference to event manager
            Instantiate(_rootUI).Init(_eventManager);
            
            //developer logo and first loading
            var splashScreen = new ActionState
            {
                Entered = () =>
                {
                    _eventManager.MainMenuEvents.SplashScreenAnimationFinished += _game.StateMachine.Context.Stop;
                    _eventManager.MainMenuEvents.SplashScreenOpen?.Invoke();
                },
                Exited = () =>
                {
                    _eventManager.MainMenuEvents.SplashScreenAnimationFinished -= _game.StateMachine.Context.Stop;
                }
            };
            
            // This also handles persona tag creation. Beta key entry is optional.
            var betaEntryScreen = new BetaKeyEntryScreen
            {
                Events = _eventManager,
                Player = _player,
                CommunicationsManager = _communicationsManager,
                GameStorageManager = _storageManager,
                IsBetaKeyRequired = _willRequireBetaKey
            };
            
            _cloudProver = new CloudProver(_cloudProverUri);
            
            // The mapper handles EC hashing, perlin noise calculations, and tells us what's there.
            var mapper = new SpaceMapper(_gameConfig);
            var map = new SpaceMap(mapper, _gameConfig, _eventManager);
            
            _eventManager.GeneralEvents.SectorExplored += sector => _ = _storageManager.TryWriteToHistory(sector);
            
            // Can find a home world and load from storage.
            var loader = new Loader(_gameConfig, _storageManager, _eventManager, _player, mapper, map);
            
            InitializeSpaceMapVisuals(map, loader);

            // Setup space view for gameplay state.
            GameObject spaceView = null;

            _eventManager.GameplayEvents.GameplayEntered += () =>
            {
                spaceView ??= Instantiate(_spaceViewPrefab, _spaceViewPrefab.transform.position, Quaternion.identity);
                spaceView.transform.SetParent(Camera.main.transform, worldPositionStays: false);
            };
            
            // This is currently the main game state.
            var gameplay = new Gameplay(
                _gameplayConfig,
                _eventManager,
                _player,
                map,
                inputActions.Gameplay, 
                Camera.main,
                _overlayController,
                _transportShipConfig,
                _planetAssetConfigs,
                worldClock,
                instanceInfo
            );
            
            splashScreen.AddExitTransition(playerHasClaimedPersonaTag ? loader : betaEntryScreen);
            betaEntryScreen.AddExitTransition(loader);
            loader.AddTransition(gameplay, () => loader.IsComplete);
            
            _game.Start(splashScreen);
            
            if (_willRespectCountdown)
                Countdown(instanceInfo, worldClock);
        }
        
        private async void Countdown(GameInstanceInfo instanceInfo, WorldClock worldClock)
        {
            bool stillNeedsToCheckRank = true;
            
            while (!Application.exitCancellationToken.IsCancellationRequested)
            {
                for (float t = 0f; t < 1f; t += Time.deltaTime)
                    await Task.Yield();
                
                double secondsRemaining = Math.Max(0, instanceInfo.SecondsPerGame - worldClock.SecondsElapsed);
                
                if (stillNeedsToCheckRank)
                {
                    stillNeedsToCheckRank = false;
                    
                    void OnPlayerLeaderStatUpdated(PlayerRankReply reply, string playerTag)
                    {
                        if (_player.Rank <= (Application.isEditor ? 10_000_000 : 10))
                        {
                            _game.StateMachine.PushState(new WinnerPopupDialog(_eventManager, _player, exitCallback: null));
                        }
                        
                        _eventManager.GameplayEvents.PlayerLeaderStatUpdated -= OnPlayerLeaderStatUpdated;
                    }

                    _eventManager.GameplayEvents.PlayerLeaderStatUpdated += OnPlayerLeaderStatUpdated;
                    _eventManager.GameplayEvents.PlayerLeaderStatUpdateRequested?.Invoke(_player.PersonaTag);
                }
                
                _eventManager.GameplayEvents.InstanceTimerUpdated?.Invoke(secondsRemaining);
            }
        }
        
        // Would prefer not to use OnGUI, but it gets the job done and meets our needs for quick debug text rendering.
        private void OnGUI() => _overlayController?.ProcessImGuiCommands();
        
        // Note: It's ok to call Quit more than once.
        private void OnDestroy()
        {
            Debug.Log($"Game Quitting.");
            Cursor.visible = true;
            
            _eventManager.GeneralEvents.SectorChanged = null;
            
            // We have to check for null now because we've introduced the possibility of an early exit during the bootstrapping phase.
            
            if (_spaceMapRT != null)
            {
                _spaceMapRT.Release();
                Destroy(_spaceMapRT);
            }
            
            _game?.Quit();
            _storageManager?.Dispose();
            s_wasCreated = false;
        }
        
        // WebGL builds don't support texture copies, but they do support fullscreen blitting, thus the awful workaround we're forced to use.
        // Please note: This restriction will change with WebGPU support for Unity Web builds.
        private void InitializeSpaceMapVisuals(SpaceMap map, Loader loader)
        {
            Assert.IsTrue(map.WorldRadius <= 8192);

            int mapLength = Mathf.Min(16384, Mathf.NextPowerOfTwo((int)map.WorldRadius * 2));

            Debug.Log($"Creating {mapLength}x{mapLength} space map texture.");

            _spaceMapRT = new RenderTexture(mapLength, mapLength, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _spaceMapRT.Create();
            _spaceMapMaterial.SetTexture(_spaceMapId, _spaceMapRT);
            
            Texture2D texelTexture = Texture2D.blackTexture;
            GameObject spaceAreaDisplay = Instantiate(_spaceMapQuadPrefab, Vector3.zero, Quaternion.identity);
            spaceAreaDisplay.GetComponent<Renderer>().material = _spaceMapMaterial;
            spaceAreaDisplay.transform.localScale = new Vector3(mapLength, mapLength, 1);
            spaceAreaDisplay.SetActive(false);
            
            _eventManager.GameplayEvents.GameplayEntered += () => spaceAreaDisplay.SetActive(true);

            int worldSizeId = Shader.PropertyToID("_WorldSize");
            int texelPositionId = Shader.PropertyToID("_TexelPosition");
            int colorId = Shader.PropertyToID("_Color");
            var texelMaterial = new Material(_texelMaterial);

            texelMaterial.SetVector(worldSizeId, Vector2.one * mapLength);

            _eventManager.GeneralEvents.LoadingFinished += () =>
            {
                _eventManager.GeneralEvents.SectorChanged += sector =>
                {
                    Vector2 p = sector.Position;
                    Color c = new Color32(0, 0, 0, 255);
                    c[(int)sector.Environment] = 255;
                    texelMaterial.SetVector(texelPositionId, p);
                    texelMaterial.SetColor(colorId, c);
                    Graphics.Blit(texelTexture, _spaceMapRT, texelMaterial);
                };

                Debug.Log($"Creating {mapLength}x{mapLength} bigBlitTexture.");

                var bigBlitTexture = new Texture2D(mapLength, mapLength, TextureFormat.ARGB4444, false, false, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                
                var textureCopyMaterial = new Material(_textureCopyMaterial)
                {
                    mainTexture = bigBlitTexture
                };
                
                Color32[] data = bigBlitTexture.GetPixels32();
                int halfLength = mapLength >> 1;
                var offset = new Vector2Int(halfLength, halfLength);
                var rect = new RectInt(0, 0, mapLength, mapLength);
                int yShift = math.ceillog2(mapLength);
                Color32 c = new Color32(0, 0, 0, 255);

                Debug.Log("Clearing bigBlitTexture with: RGBA: 0,0,0,1");

                for (int i = 0; i < data.Length; ++i)
                    data[i] = c;

                Debug.Log("Writing history into array.");

                foreach (var sector in loader.History)
                {
                    var p = sector.Position + offset;

                    if (!rect.Contains(p))
                        continue;
                    
                    c = new Color32(0, 0, 0, 255);
                    c[(int)sector.Environment] = 255;
                    data[(p.y << yShift) + p.x] = c;
                }

                Debug.Log("Writing history array to bigBlitTexture.");
                bigBlitTexture.SetPixels32(data);

                Debug.Log("Uploading bigBlitTexture to VRAM.");
                bigBlitTexture.Apply();

                Debug.Log($"Blitting bigBlitTexture from VRAM to {nameof(_spaceMapRT)}.");
                Graphics.Blit(bigBlitTexture, _spaceMapRT, textureCopyMaterial);

                Debug.Log("Scheduling bigBlitTexture for destruction.");
                Destroy(bigBlitTexture);

                Debug.Log("History fully loaded.");
            };
        }
        
        private void InitializeOnlineEvents()
        {
            // ReSharper disable once AsyncVoidLambda - Decent broadly scoped advice, but this is fine.
            _eventManager.GameplayEvents.SendEnergyRequested += async (positionFrom, positionTo, msg) =>
            {
                (ProofResponse response, bool wasSuccessful) = await _cloudProver.MoveRequest(
                    positionFrom, positionTo,
                    msg.locationHashFrom, msg.locationHashTo,
                    msg.perlinTo,
                    _gameConfig,
                    Application.exitCancellationToken);

                if (!wasSuccessful)
                {
                    Debug.LogWarning($"CLOUD PROVER 'move' request FAILED prior to SendEnergyAsync.");
                    return;
                }
                
                Debug.Log($"CLOUD PROVER 'move' request SUCCEEDED prior to SendEnergyAsync.");
                
                // All good, so now pass to the RPC side of things.
                msg.proof = response.proof;
                
                SocketResponse<SendEnergyReceipt> result = await _communicationsManager.SendEnergyAsync(msg, Application.exitCancellationToken);
                Debug.Log($"tx-send-energy IsComplete? {result.IsComplete}");

                if (!result.IsComplete)
                    return;
                
                Debug.Log($"LocationHash: {result.Content.Planet.LocationHash} Owner: {result.Content.Ship.OwnerPersonaTag} ShipId: {result.Content.Ship.Id}");
                _eventManager.GameplayEvents.SendEnergyApproved?.Invoke(result.Content);
            };
            
            // ReSharper disable once AsyncVoidLambda - Decent broadly scoped advice, but this is fine.
            _eventManager.GameplayEvents.HomePlanetClaimRequested += async (p, msg) =>
            {
                (ProofResponse response, bool wasSuccessful) = await _cloudProver.InitRequest(p, msg.locationHash, msg.perlin, _gameConfig, Application.exitCancellationToken);

                if (!wasSuccessful)
                {
                    Debug.LogWarning($"CLOUD PROVER 'init' request FAILED prior to ClaimHomePlanetAsync.");
                    _eventManager.GameplayEvents.HomePlanetClaimDenied?.Invoke();

                    return;
                }
                
                Debug.Log($"CLOUD PROVER 'init' request SUCCEEDED prior to ClaimHomePlanetAsync.");
                
                // All good, so now pass to the RPC side of things.
                msg.proof = response.proof;
                
                SocketResponse<ClaimHomePlanetReceipt> result = await _communicationsManager.ClaimHomePlanetAsync(msg, Application.exitCancellationToken);
                Debug.Log($"tx-claim-home-planet IsComplete? {result.IsComplete}");
                
                if (!result.IsComplete)
                {
                    _eventManager.GameplayEvents.HomePlanetClaimDenied?.Invoke();
                    return;
                }

                Debug.Log($"{_player.PersonaTag} <-- same? --> {result.Content.Planet.OwnerPersonaTag}");
                
                if (result.Content.HasErrors || result.Content.Player.PersonaTag != _player.PersonaTag)
                {
                    if (result.Content.HasErrors)
                        Debug.Log(string.Join('\n', result.Content.Errors));
                    
                    if (result.Content.Player.PersonaTag != _player.PersonaTag)
                        Debug.Log($"result.Content.Player.PersonaTag ({result.Content.Player.PersonaTag}) != player.PersonaTag ({_player.PersonaTag})");
                    
                    _eventManager.GameplayEvents.HomePlanetClaimDenied?.Invoke();
                }
                else
                {
                    _eventManager.GameplayEvents.HomePlanetClaimApproved?.Invoke();
                }
            };
            
            // ReSharper disable once AsyncVoidLambda - Decent broadly scoped advice, but this is fine.
            _eventManager.GameplayEvents.DebugEnergyBoosted += async msg =>
            {
                RpcResult result = await _communicationsManager.DebugEnergyBoostAsync(msg, Application.exitCancellationToken);
                Debug.Log($"tx-debug-energy-boost WasSuccessful? {result.WasSuccessful}");
            };
            
            // ReSharper disable once AsyncVoidLambda - Decent broadly scoped advice, but this is fine.
            _eventManager.GameplayEvents.PlanetsUpdateRequested += async planets =>
            {
                List<string> planetHashes = ListPool<string>.Get();
                planetHashes.Clear();
                
                // It's important that we remove the dependency on the planets collection immediately.
                // If we wait, planets could be modified by the time we get to the RPC call.
                // The way we're currently handling it is fine, but just be aware.
                foreach (Planet planet in planets)
                {
                    if (planet?.LocationHash is not null)
                        planetHashes.Add(planet.LocationHash);
                    else
                        Debug.LogWarning($"Planet.MimcHash is null for: {planet}");
                }
                
                RpcResult result = await _communicationsManager.ReadPlanetsAsync(new CurrentStateMsg { planetsList = planetHashes }, Application.exitCancellationToken);
                
                ListPool<string>.Release(planetHashes);

                if (!result.WasSuccessful)
                    return;
                
                Debug.Log(result.Payload);

                var reply = JsonUtility.FromJson<CurrentStateReply>(result.Payload);
                PlanetData[] incomingPlanets = reply.planets;

                Assert.IsNotNull(incomingPlanets);
                Debug.Log($"Incoming planet count: {incomingPlanets.Length}");

                // This is the null check mentioned below.
                if (_eventManager.GameplayEvents.PlanetUpdateReceived is null)
                {
                    Debug.LogWarning("_eventManager.GameplayEvents.PlanetUpdateReceived is null.");
                    return;
                }
                    
                // I don't really like this, but it's a quick fix for a precise problem.
                // Gameplay will set this up but isn't ready when history replay occurs.
                if (incomingPlanets.Length > 0)
                {
                    // Please notice the null check above this. We do check.
                    bool noSubscribers = _eventManager.GameplayEvents.PlanetUpdateReceived.GetInvocationList().Length == 0;
                        
                    if (noSubscribers)
                        Debug.Log($"Awaiting a subscription to: {nameof(_eventManager.GameplayEvents.PlanetUpdateReceived)}");
                        
                    // Please notice the null check above this. We do check.
                    while ((_eventManager.GameplayEvents.PlanetUpdateReceived.GetInvocationList().Length == 0) && !Application.exitCancellationToken.IsCancellationRequested)
                        await Task.Yield();
                        
                    if (noSubscribers)
                        Debug.Log($"Something subscribed to: {nameof(_eventManager.GameplayEvents.PlanetUpdateReceived)}");
                }

                _eventManager.GameplayEvents.PlanetUpdateReceived?.Invoke(incomingPlanets);
            };

            //player leaderboard stat update
            // ReSharper disable once AsyncVoidLambda
            _eventManager.GameplayEvents.PlayerLeaderStatUpdateRequested += async (playerTag) =>
            {
                if (playerTag is null)
                {
                    Debug.LogError("playerTag is null");
                    return;
                }

                RpcResult resp = await _communicationsManager.ReadPlayerRankAsync(new PlayerRankMsg { personaTag = playerTag }, Application.exitCancellationToken);

                if (Application.exitCancellationToken.IsCancellationRequested)
                    return;
                
                if (resp.WasSuccessful)
                {
                    PlayerRankReply reply = JsonUtility.FromJson<PlayerRankReply>(resp.Payload);
                    _player.Rank = (int)reply.rank;
                    _player.Score = (int)reply.score;
                    _eventManager.GameplayEvents.PlayerLeaderStatUpdated?.Invoke(reply, _player.PersonaTag);
                }
                else
                {
                    // TODO: Handle this as needed.
                    Debug.LogError(resp.Error.message ?? $"{nameof(_communicationsManager.ReadPlayerRankAsync)} failed.");
                }
            };
            
            //player leaderboard stat update
            // ReSharper disable once AsyncVoidLambda
            _eventManager.GameplayEvents.LeaderboardUpdateRequested += async (playerTag) =>
            {
                //for now we always only get the rank 1-50, in the backend they use 0-indexed so we request for player in starting position 0 to 49
                RpcResult resp = await _communicationsManager.ReadLeaderboardAsync(new LeaderboardRangeMsg() { start = 0, end = 49 }, Application.exitCancellationToken); 
                
                if (Application.exitCancellationToken.IsCancellationRequested)
                    return;
                
                if (resp.WasSuccessful)
                {
                    LeaderboardReply reply = JsonUtility.FromJson<LeaderboardReply>(resp.Payload);
                    _eventManager.GameplayEvents.LeaderboardUpdated?.Invoke(reply, playerTag);
                }
                else
                {
                    // TODO: Handle this as needed.
                    Debug.LogError(resp.Error.message);
                }
            };
            
            // ReSharper disable once AsyncVoidLambda
            _eventManager.GeneralEvents.LinkNewEvmAddress += async (newAddress) =>
            {
                if (_linkedEvmAddressStorageManager.EvmAddresses.Contains(newAddress))
                {
                    _eventManager.GeneralEvents.LinkNewEvmAddressCompleted?.Invoke(false);
                    return;
                }
                
                RpcResult resp = await _communicationsManager.AuthorizePersonaAddressAsync(new AuthorizePersonaAddressMsg() { personaTag = _player.PersonaTag, address = newAddress });
                
                if (Application.exitCancellationToken.IsCancellationRequested)
                    return;

                if ((!resp.WasSuccessful || !_linkedEvmAddressStorageManager.TryStoreEvmAddress(newAddress)))
                    Debug.LogError("link new evm address failed");
                
                _eventManager.GeneralEvents.LinkNewEvmAddressCompleted?.Invoke(resp.WasSuccessful);
            };

            _eventManager.GeneralEvents.RequestLinkedEvmList += () =>
            {
                _eventManager.GeneralEvents.RespondLinkedEvmList?.Invoke(_linkedEvmAddressStorageManager.EvmAddresses.ToArray());
            };

            _eventManager.GeneralEvents.RequestPlayerTag += () =>
            {
                _eventManager.GeneralEvents.RespondPlayerTag?.Invoke(this._player.PersonaTag);
            };
        }

        private static void Panic(string message)
        {
            Debug.LogError(message);
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}