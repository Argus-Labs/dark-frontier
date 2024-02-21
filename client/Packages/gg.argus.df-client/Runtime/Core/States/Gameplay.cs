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
using System.Text;
using ArgusLabs.DF.Core.Communications;
using ArgusLabs.DF.Core.Configs;
using ArgusLabs.DF.Core.Space;
using ArgusLabs.DF.Input;
using ArgusLabs.DF.UI.Utilities;
using ArgusLabs.WorldEngineClient.Communications;
using Smonch.CyclopsFramework;
using Smonch.CyclopsFramework.Extensions;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using static Unity.Mathematics.math;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace ArgusLabs.DF.Core.States
{
    // Scope notes: This is instantiated in Bootstrap and nothing else has access, even through Bootstrap.
    public sealed class Gameplay : CyclopsGameState
    {
        private const string ExplorerTag = "explorer";
        private const string LeaderboardTag = "leaderboard";
        private const string ZoomTag = "zoom";
        
        private const float OrthoStepScale = 1.618f;
        private const float MinOrthoSize = 1.618f;
        
        // Dependencies
        private readonly GameplayConfig _gameplayConfig;
        private readonly EventManager _eventManager;
        private readonly Player _player;
        private readonly SpaceMap _map;
        private DfInputActions.GameplayActions _gameplayInput;
        private readonly Camera _spaceCamera;
        private readonly OverlayController _overlay;
        private readonly MeshAssetsConfig _transportShipConfig;
        private readonly PlanetAssetsConfig[] _planetAssetConfigs;
        private readonly WorldClock _worldClock;
        private readonly GameInstanceInfo _worldInstanceInfo;
        
        private CyclopsStateMachine _inputStateMachine;
        private Planet _homePlanet;
        private Sector _homeSector;
        private Explorer _explorer;
        private bool _isPlacingExplorer;
        private Planet _hitTestResultPlanet;
        private Vector2 _hitTestPosition;
        private Vector2 _selectionWorldPosition;
        private Planet _targetedPlanet;
        private Planet _selectedPlanet;
        private double _energyTransferScale = 1.0;
        private float _zoomProgress;

        private enum DebugMode
        {
            Inactive = 0,
            Basic,
            Extra,
            BasicWithPlanets,
            ExtraWithPlanets
        }

        private DebugMode _debugMode = DebugMode.Basic;

        private RenderBatcher[] _renderBatchers;
        
        private readonly HashSet<Planet> _planetsOnScreen = new(); //planets on screen means all planet that are on screen regardless of the lod
        private readonly Dictionary<string, Player> _playersByPersonaTag = new();
        private readonly HashSet<ulong> _clientEnergyTransferIds = new();
        private readonly PlayerColorManager _playerColorManager = new (maxUniqueColors: 12); // TODO: Set this via config.

        private readonly Queue<string> _errorMessageQueue = new();
        private string _mostRecentErrorMessage = string.Empty;
        
        private readonly float _squareDrawZoomLimit = 1000f; //square (grid pointer and explorer) won't be drawn when camera orthographic size is higher than this
        // Note: Please cache these values if performance is ever a concern. This is fine for now.
        private float LogMaxOrthoSize => Mathf.Log(MaxOrthoSize, 1.618f);
        private float NormalizedOrthoSize => Mathf.InverseLerp(MinOrthoSize, LogMaxOrthoSize, Mathf.Log(_spaceCamera.orthographicSize, 1.618f));
        private Vector2 MouseWorldPosition => _spaceCamera.ScreenToWorldPoint(Mouse.current.position.value);
        private Vector2Int MouseGridPosition => Vector2Int.RoundToInt(MouseWorldPosition);
        
        // We don't use this inefficiently but Rider won't stop complaining. This fixes it.
        private Transform SpaceCameraTransform { get; }
        private float ScreenScale => 1f / (.5f * (Screen.height / _spaceCamera.orthographicSize));
        
        private float MaxOrthoSize => pow(OrthoStepScale, ceil(Mathf.Log(_map.WorldRadius, OrthoStepScale)));
        
        private Bounds SpaceCameraWorldBounds
        {
            get
            {
                float height = 2f * _spaceCamera.orthographicSize;
                float width = height * _spaceCamera.aspect;
                return new Bounds(SpaceCameraTransform.position.XY(), new Vector2(width, height));
            }
        }
        
        private Vector2 TargetedWorldPosition => _targetedPlanet?.Position ?? MouseWorldPosition;
        
        private Planet SelectedPlanet
        {
            get => _selectedPlanet;

            set
            {
                if ((_selectedPlanet is not null) && (_selectedPlanet == value))
                    return;
                
                if (_selectedPlanet is not null)
                    _eventManager.GameplayEvents.PlanetDeselected?.Invoke();

                _selectedPlanet = value;

                if (_selectedPlanet is null)
                    return;
                
                _eventManager.GameplayEvents.PlanetSelected?.Invoke(_selectedPlanet, _player.Owns(_selectedPlanet), _map[_selectedPlanet.Position].Environment);
                Debug.Log($"Selected: {_selectedPlanet}");
            }
        }
        
        public Gameplay(
            GameplayConfig gameplayConfig,
            EventManager eventManager,
            Player player,
            SpaceMap map,
            DfInputActions.GameplayActions gameplayInput,
            Camera spaceCamera,
            OverlayController overlay,
            MeshAssetsConfig transportShipConfig,
            PlanetAssetsConfig[] planetAssetConfigs,
            WorldClock worldClock,
            GameInstanceInfo worldInstanceInfo)
        {
            _gameplayConfig = gameplayConfig;
            _eventManager = eventManager;
            _player = player;
            _map = map;
            _gameplayInput = gameplayInput;
            _spaceCamera = spaceCamera;
            _overlay = overlay;
            _transportShipConfig = transportShipConfig;
            _planetAssetConfigs = planetAssetConfigs;
            _worldClock = worldClock;
            _worldInstanceInfo = worldInstanceInfo;
            
            // We don't use this inefficiently but Rider won't stop complaining. This fixes it.
            SpaceCameraTransform = _spaceCamera.transform;
        }
        
        // Note: We're not handling re-entry. This state will enter and exit once only.
        protected override void OnEnter()
        {
            Debug.Log($"{nameof(Gameplay)} {nameof(OnEnter)}");
            
            // These RenderBatchers reduce draw calls by batching planets with the same material.
            // This really means we're just batching planets of the same level.
            _renderBatchers = new RenderBatcher[_planetAssetConfigs.Length];
            
            for (int i = 0; i < _planetAssetConfigs.Length; ++i)
            {
                PlanetAssetsConfig cfg = _planetAssetConfigs[i];
                var surfaceRenderParams = new RenderParams(cfg.SurfaceMaterial);
                Mesh planetMesh = cfg.Mesh;

                _renderBatchers[i] = new RenderBatcher(planetMesh, surfaceRenderParams);
            }
            
            // Instantiate and position the Explorer.
            _explorer = new Explorer
            {
                Position = _player.HomePosition,
                IsExploring = true
            };
            
            // Ensure current starting position is explored.
            _homeSector = _map.Explore(_explorer.Position);
            _eventManager.GeneralEvents.SectorChanged?.Invoke(_homeSector);
            
            // It's ok if they no longer own this. They'll start here anyway.
            _homePlanet = _homeSector.Planet;
            _eventManager.GameplayEvents.PlanetControlChanged?.Invoke(_homePlanet, _player, Player.Alien);
            
            // Translate camera to home world position.
            PanSpaceCamera(_player.HomePosition);
            FocusOnPlanet(_homePlanet);
            
            _gameplayInput.Enable();
            
            InitializeStates();
            InitializeExplorer();
            InitializePlanetSynchronization();
            InitializeLeaderboard();
            
            // Continuously show the current grid hovered by mouse
            Engine.NextFrame.Loop(() =>
            {
                if (!_spaceCamera.pixelRect.Contains(Mouse.current.position.value))
                    return;
                
                Sector sector = _map[MouseGridPosition];
                _eventManager.GameplayEvents.GridHovered?.Invoke(sector);
            }).AddTag("MouseOverSector");

            Engine
                .NextFrame.Sleep(_gameplayConfig.WorldClockResyncPeriod).AddTag("WorldClockResync")
                .Next.Loop(_gameplayConfig.WorldClockResyncPeriod, float.MaxValue, _worldClock.Resync);
            
            // Update And Render Space View ////////////////////////////////////

            Engine.NextFrame.Loop(UpdatePlanets).AddTag(nameof(UpdatePlanets));
            Engine.NextFrame.Loop(AlignView).AddTag(nameof(AlignView));
            
            _eventManager.GameplayEvents.PlanetInfoClosed += () => SelectedPlanet = null;
            _eventManager.GameplayEvents.PlanetIndexSelectPlanet += gridPos =>
            {
                SelectedPlanet = _map[gridPos].Planet;

                if (SelectedPlanet is null)
                    return;
                
                _selectionWorldPosition = gridPos;
                FocusOnPlanet(SelectedPlanet);
            };
            
            _eventManager.GameplayEvents.EnergyTransferPercentUpdated += percent => _energyTransferScale = percent / 100.0;
            
            // Occurs when we've received approval from the backend.
            _eventManager.GameplayEvents.SendEnergyApproved += OnSendEnergyTransfer;
            
            _eventManager.GameplayEvents.GameplayEntered?.Invoke();
        }
        
        protected override void OnExit()
        {
            // Clearing in this manner should be safe for this use case.
            _eventManager.GameplayEvents.ExplorerStarting = null;
            _eventManager.GameplayEvents.ExplorerStopping = null;
            _eventManager.GameplayEvents.ExplorerStartRelocating = null;
            _eventManager.GameplayEvents.ExplorerStopRelocating = null;
            _eventManager.GameplayEvents.PlanetInfoClosed = null;
            _eventManager.GameplayEvents.PlanetIndexSelectPlanet = null;
            _eventManager.GameplayEvents.PlanetShown = null;
            _eventManager.GameplayEvents.PlanetHidden = null;
            _eventManager.GameplayEvents.PlanetSelected = null;
            _eventManager.GameplayEvents.PlanetDeselected = null;
            _eventManager.GameplayEvents.LeaderboardOpened = null;
            _eventManager.GameplayEvents.LeaderboardClosed = null;

            _gameplayInput.Move.Disable();
            _eventManager.GameplayEvents.GameplayExited?.Invoke();
        }
        
        private void InitializeStates()
        {
            // State Actions
            
            // The targeted planet is the one we're currently hovering over but may not have selected.
            void TargetPlanet()
            {
                if (_targetedPlanet is not null)
                    _eventManager.GameplayEvents.PlanetHoverEnded?.Invoke();

                _targetedPlanet = _hitTestResultPlanet;

                if (_targetedPlanet is not null)
                    _eventManager.GameplayEvents.PlanetHoverStarted?.Invoke(_targetedPlanet, _player.Owns(_targetedPlanet));
            }
            
            void ShowHoveredGrid()
            {
                if (!(_spaceCamera.orthographicSize < _squareDrawZoomLimit) || !(Mouse.current.position.value.x > 0) || !(Mouse.current.position.value.x < Screen.width)
                    || !(Mouse.current.position.value.y > 0) || !(Mouse.current.position.value.y < Screen.height)) return;
                
                var targetWorldPosition = (Vector3)round(_spaceCamera.ScreenToWorldPoint(Mouse.current.position.value));
                _overlay.DrawWorldSquare(new Vector3(targetWorldPosition.x, targetWorldPosition.y, 0), 1f, .1f, ColorPalette.Explorer);
            }
            
            void Zoom()
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return; //make zoom not working if mouse is on top of a ui component
                
                float zoomDir = -sign(Mouse.current.scroll.value.y);
                bool isZoomingOut = zoomDir > 0f;
                
                if (!isZoomingOut && (MouseWorldPosition.sqrMagnitude > _map.WorldRadius * _map.WorldRadius))
                    return;

                if (Mathf.Approximately(zoomDir, 0f))
                    return;
                
                float scale = isZoomingOut ? OrthoStepScale : 1f / OrthoStepScale;
                Vector3 cursorWorldPosition = _spaceCamera.ScreenToWorldPoint(Mouse.current.position.value);
                Vector3 offset = (cursorWorldPosition - _spaceCamera.ViewportToWorldPoint(new Vector3(.5f, .5f, 0f)));
                float desiredOrthoSize = _spaceCamera.orthographicSize * scale;

                if (Mathf.Approximately(desiredOrthoSize, MinOrthoSize))
                    desiredOrthoSize = MinOrthoSize;
                else if (Mathf.Approximately(desiredOrthoSize, MaxOrthoSize))
                    desiredOrthoSize = MaxOrthoSize;

                if ((desiredOrthoSize <= MinOrthoSize) || (desiredOrthoSize >= MaxOrthoSize))
                    if (Engine.Exists(ZoomTag))
                        return;
                
                desiredOrthoSize = clamp(desiredOrthoSize, MinOrthoSize, MaxOrthoSize);

                if (Mathf.Approximately(desiredOrthoSize, _spaceCamera.orthographicSize))
                    return;

                Vector3 cameraStartingPosition = SpaceCameraTransform.position;
                Vector3 zoomTargetPosition = cameraStartingPosition + offset - (offset * scale);

                float startingOrthoSize = _spaceCamera.orthographicSize;

                Engine.NextFrame.Lerp(1.0 / 3.0, maxCycles: 1.0, bias: Easing.EaseOut5, f: t =>
                {
                    SpaceCameraTransform.position = lerp(cameraStartingPosition, zoomTargetPosition, t);
                    _spaceCamera.orthographicSize = clamp(lerp(startingOrthoSize, desiredOrthoSize, t),
                        MinOrthoSize, MaxOrthoSize);

                    _eventManager.GameplayEvents.CameraZoomChanged?.Invoke(_spaceCamera.orthographicSize);
                }).AddTag(ZoomTag)
                .Next
                .Add(() =>
                {
                    if (_explorer.IsExploring)
                        Engine.Resume(ExplorerTag);
                });
                
                // Zooming is smooth when the explorer isn't running, so we're pausing the explorer while zooming.
                // An improvement to the zooming method may prevent the excessive jitter as well, so perhaps we should try that instead of pausing the explorer.
                Engine.Pause(ExplorerTag);
            }
            
            // Move handles camera positioning based on mouse and keyboard input. This includes zoom.
            void Move()
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                if ((_targetedPlanet is not null || SelectedPlanet is not null) && Keyboard.current.fKey.wasPressedThisFrame)
                {
                    Planet planet = _targetedPlanet ?? SelectedPlanet;
                    FocusOnPlanet(planet);
                    return;
                }

                if (Keyboard.current.hKey.isPressed)
                {
                    FocusOnPlanet(_homePlanet);
                    return;
                }

                if (Keyboard.current.xKey.wasPressedThisFrame)
                {
                    Focus((Vector2)_explorer.Position, 0.4f);
                    return;
                }

                //each frame camera can only be controlled by mouse OR keyboard, so we check if middle mouse is pressed first here
                if ((Mouse.current.middleButton.isPressed && !Mouse.current.middleButton.wasPressedThisFrame) || (Mouse.current.rightButton.isPressed && !Mouse.current.rightButton.wasPressedThisFrame))
                {
                    // middle mouse drag movement

                    Vector2 dragDelta = _spaceCamera.ScreenToWorldPoint(Mouse.current.position.ReadValueFromPreviousFrame())
                        - _spaceCamera.ScreenToWorldPoint(Mouse.current.position.value);

                    if (dragDelta.sqrMagnitude > 0f)
                        PanSpaceCamera(dragDelta);
                }
                else
                {
                    //keyboard movement

                    Vector3 move = _gameplayInput.Move.ReadValue<Vector2>();
                    
                    // If the mouse is moving, the user may be trying to move the mouse cursor out of the game window.
                    // So to prevent that annoyance, only scroll the viewport when the mouse cursor is still.
                    // Editor only. We're fullscreen in the browser.
                    if (Screen.fullScreen && (Mathf.Approximately(0f, Mouse.current.delta.value.sqrMagnitude) || !Application.isEditor))
                    {
                        // Test cursor against edge of screen.
                        Vector2 mpn = _spaceCamera.ScreenToViewportPoint(Mouse.current.position.value);
                        Vector3 dir = Vector3.zero;
                        float margin = .02f;
                        var aabb = new Rect(0f, 0f, 1f, 1f);

                        if (aabb.Contains(mpn))
                        {
                            if (mpn.x < margin || mpn.x > (1f - margin))
                                dir.x = saturate(round(mpn.x)) * 2f - 1f;
                            if (mpn.y < margin || mpn.y > (1f - margin))
                                dir.y = saturate(round(mpn.y)) * 2f - 1f;

                            move += dir;
                        }
                    }

                    move *= _spaceCamera.orthographicSize * Time.deltaTime * (2f - pow(NormalizedOrthoSize, 2f)) * 2f;
                    PanSpaceCamera(move);
                }
            }
            
            // Assumes that we're selecting planets of course. If that changes, please update this.
            void Select()
            {
                if (EventSystem.current.IsPointerOverGameObject() || !_gameplayInput.Select.IsPressed())
                    return;
                
                SelectedPlanet = _hitTestResultPlanet;
                _selectionWorldPosition = _hitTestPosition;
            }

            void Relocate()
            {
                //pressing anything during relocating will result in exiting the relocating state, including any ui component
                if (!_gameplayInput.Select.WasPressedThisFrame())
                    return;
                
                _isPlacingExplorer = false;

                if (EventSystem.current.IsPointerOverGameObject())
                    return;
                    
                _selectionWorldPosition = round(MouseWorldPosition);
                Vector2Int desiredExplorerPosition = Vector2Int.FloorToInt(_selectionWorldPosition);

                if (!_map.IsValidPosition(desiredExplorerPosition))
                {
                    _eventManager.GameplayEvents.ExplorerInvalidRelocationAttempted?.Invoke();
                    return;
                }
                
                _explorer.Reset();
                _explorer.Position = desiredExplorerPosition;
                _ = _map.Explore(_explorer.Position);
            }
            
            void DragForEnergyTransfer()
            {
                var line = new float4(_selectionWorldPosition, TargetedWorldPosition);
                var energyTransferPointer = new EnergyLine { Line = line, Color = ColorPalette.LocalEnergyLine };

                //checking energy sending distance should still uses their grid position, or otherwise the estimation will change with small mouse movement
                Vector2Int srcCellPosition = Vector2Int.RoundToInt(_selectionWorldPosition);
                Vector2Int dstCellPosition = Vector2Int.RoundToInt(TargetedWorldPosition);
                Planet source = SelectedPlanet;
                
                if (!_player.Owns(source))
                    return;
                
                // This is validated at the state level, but checking anyway.
                if (Engine.Exists(source.PendingEnergyTransferTag))
                {
                    Debug.LogWarning($"Pending transfers should be filtered out at the state transition level.");
                    return;
                }
                
                double d = (srcCellPosition - dstCellPosition).magnitude; // <-- Yeah, that's a float. It's fine.
                double energySent = source.EnergyLevel * _energyTransferScale; //the _energyTransferScale have been validated on the control ui
                double travelCostPerUnit = source.EnergyCapacity * .95 / source.FullRange;
                double travelCost = d * travelCostPerUnit + source.EnergyCapacity * .05;
                int energyToBeReceived = (int)(energySent - travelCost);
                double travelTimeInSeconds = d / source.Speed;

                // This should probably use a delegate or take struct. 3 args is a bit clunky for an Action.
                _eventManager.GameplayEvents.EnergyTransferPreview?.Invoke(
                    UIUtility.GetTransferInfoArmRotation(TargetedWorldPosition - _selectionWorldPosition),
                    TimeSpan.FromSeconds(travelTimeInSeconds),
                    energyToBeReceived);
                
                bool mustExitEarly = false;
                
                //give net transfer preview when hovering on a planet
                if (_map[dstCellPosition].TryGetPlanet(out Planet destination))
                {
                    // Return if planet is null or the source and destination match.
                    if (destination is null || destination == source)
                    {
                        mustExitEarly = true;
                    }
                    else
                    {
                        int netReceivedEnergy = _player.Owns(destination)
                            ? energyToBeReceived
                            : Mathf.RoundToInt(energyToBeReceived / (float)destination.Defense * 100f);
                        
                        _eventManager.GameplayEvents.EnergyTransferTargetHoverStart?.Invoke(destination.Id, _player.Owns(destination), netReceivedEnergy);
                    }
                }
                else
                {
                    _eventManager.GameplayEvents.EnergyTransferTargetHoverStop?.Invoke();
                    mustExitEarly = true;
                }

                if ((_selectionWorldPosition - TargetedWorldPosition).magnitude > (float)source.GetCurrentRange(_energyTransferScale) || source.ExportShipRemaining == 0)
                {
                    energyTransferPointer.Color = Color.red;
                    mustExitEarly = true;
                }

                if (Mouse.current.leftButton.isPressed && !Mouse.current.leftButton.wasReleasedThisFrame)
                    mustExitEarly = true;

                if (mustExitEarly)
                {
                    DrawCandidateTransfer(energyTransferPointer, 1f);
                    return;
                }

                _eventManager.GameplayEvents.EnergyTransferTargetHoverStop?.Invoke(); //mouse released, hover stop
                
                line = new float4((Vector2)srcCellPosition, (Vector2)dstCellPosition); //use the rounded cell position so the line won't be weirdly hanging to a space where mouse is released

                //create placeholder empty line that will automatically timeout.
                var placeholderLine = new EnergyLine { Color = energyTransferPointer.Color, Line = line};
                const double timeout = 15.0;
                
                Engine
                    .NextFrame.Lerp(period: timeout, maxCycles: 1.0, _ => DrawCandidateTransfer(placeholderLine, .4f))
                    .AddTag(source.PendingEnergyTransferTag);
                
                Debug.Log($"Expected prior to Receipt: energySent: {energySent}");
                
                _eventManager.GameplayEvents.SendEnergyRequested?.Invoke(
                    source.Position,
                    destination.Position,
                    new SendEnergyMsg
                    {
                        energy = (int)energySent, // changed from: energyToBeReceived,
                        locationHashFrom = source.LocationHash, // _mapper.MimcHashValueAt(source.Position),
                        locationHashTo = destination.LocationHash, // _mapper.MimcHashValueAt(destination.Position),
                        maxDistance = (int)ceil((source.Position - destination.Position).magnitude),
                        perlinTo = destination.PerlinValue, // _mapper.PerlinValueAt(destination.Position),
                        radiusTo = _map.WorldRadius
                    });
            }
            
            void DrawCandidateTransfer(EnergyLine energyLine, float alpha)
            {
                //drawing the candidate line
                float2 wa = energyLine.Line.xy;
                float2 wb = energyLine.Line.zw;
                
                _overlay.DrawWorldLine(wa, wb,
                    energyLine.Color.WithAlpha(alpha),
                    _gameplayConfig.TransferPreviewLineDashScale,
                    _gameplayConfig.TransferPreviewLineGapToDashRatio);
            }

            // Request an energy boost for debugging purposes only. Spamming this may not result in a significant energy boost, but feel free to try!

            void OnEnergyBoostRequested(InputAction.CallbackContext context)
            {
                if (SelectedPlanet is null)
                {
                    Debug.Log("Please select a planet before attempting to boost energy.");
                    return;
                }
                
                _eventManager.GameplayEvents.DebugEnergyBoosted?.Invoke(new LocationHashMsg { locationHash = SelectedPlanet.LocationHash });
            }
            
            InitializeDebugView(out Action<string> displayStateInfo, out Action monitorDebugTextToggles);
            
            // Gameplay States /////////////////////////////////////////////////
            
            // These are simple classic FSM style states, but the state machine supports stacking too.
            // This free book has a great overview of state management: https://gameprogrammingpatterns.com/state.html
            
            var navigation = new CyclopsState
            {
                Entered = () => _gameplayInput.EnergyBoost.performed += OnEnergyBoostRequested,
                Updating = () =>
                {
                    Zoom();
                    Move();
                    ShowHoveredGrid();
                    TargetPlanet();
                    Select();
                    monitorDebugTextToggles();
                    displayStateInfo("navigation");
                },
                Exited = () => _gameplayInput.EnergyBoost.performed -= OnEnergyBoostRequested
            };
            
            var energyTransfer = new CyclopsState
            {
                Entered = () => _eventManager.GameplayEvents.EnergyTransferStateEntered?.Invoke(),
                Updating = () =>
                {
                    Zoom();
                    Move();
                    TargetPlanet();
                    DragForEnergyTransfer();
                    monitorDebugTextToggles();
                    displayStateInfo("energyTransfer");
                },
                Exited = () =>
                {
                    _eventManager.GameplayEvents.EnergyTransferStateExited?.Invoke();
                    DragForEnergyTransfer();
                }
            };

            var explorerRelocation = new CyclopsState
            {
                Entered = () => _eventManager.GameplayEvents.RelocatingStateChanged?.Invoke(true),
                Updating = () =>
                {
                    Zoom();
                    Move(); 
                    ShowHoveredGrid();
                    Relocate();
                    monitorDebugTextToggles();
                    displayStateInfo("explorerRelocation");
                },
                Exited = () => _eventManager.GameplayEvents.RelocatingStateChanged?.Invoke(false)
            };

            // Transitions /////////////////////////////////////////////////////
            
            var toEnergyTransfer = new CyclopsStateTransition
            {
                Target = energyTransfer,
                Condition = () =>
                {
                    bool result = _gameplayInput.Select.IsPressed()
                        && (SelectedPlanet is not null)
                        && (SelectedPlanet == _targetedPlanet)
                        && _player.Owns(SelectedPlanet)
                        && (!Engine.Exists(SelectedPlanet.PendingEnergyTransferTag));
                    
                    return result;
                }
            };
            
            navigation.AddTransition(toEnergyTransfer);
            navigation.AddTransition(explorerRelocation, () => _isPlacingExplorer);
            explorerRelocation.AddTransition(navigation, () => !_isPlacingExplorer);
            
            energyTransfer.AddTransition(navigation, () =>
            {
                if (!_gameplayInput.Select.IsPressed())
                    return true;
                
                if (SelectedPlanet is null)
                    return true;
                
                return Engine.Exists(SelectedPlanet.PendingEnergyTransferTag);
            });
            
            // Start The State Machine /////////////////////////////////////////

            // Fire it up.
            _inputStateMachine = new CyclopsStateMachine();
            _inputStateMachine.PushState(navigation);
            
            // Engine drives the machine.
            Engine.NextFrame.Loop(_inputStateMachine.Update).AddTag("InputStateMachine.Update");
        }
        
        private void InitializeExplorer()
        {
            // Explorer Loop - Tagged below with ExplorerTag for pause/resume/etc functionality.
            Engine
                .NextFrame
                .Add(new ExplorerRoutine(_explorer, _map))
                .AddTag(ExplorerTag);

            Engine.NextFrame.Loop(() =>
            {
                if (_spaceCamera.orthographicSize < _squareDrawZoomLimit)
                    _overlay.DrawWorldSquare((Vector2)(_explorer.Position), 1f, .1f, ColorPalette.Explorer);
            }).AddTag("DrawWorldSquare");
            
            _eventManager.GameplayEvents.ExplorerStartRelocating += () => _isPlacingExplorer = true;
            _eventManager.GameplayEvents.ExplorerStopRelocating += () => _isPlacingExplorer = false;
            
            _eventManager.GameplayEvents.ExplorerStarting += () =>
            {
                Engine.Resume(ExplorerTag);
                _explorer.IsExploring = true;
                _eventManager.GameplayEvents.ExplorerMoveStateChanged?.Invoke(true);
            };
            
            _eventManager.GameplayEvents.ExplorerStopping += () =>
            {
                Engine.Pause(ExplorerTag);
                _explorer.IsExploring = false;
                _eventManager.GameplayEvents.ExplorerMoveStateChanged?.Invoke(false);
            };
        }

        private void InitializePlanetSynchronization()
        {
            var planetSet = new HashSet<Planet>();
            var planetQueue = new Queue<Planet>();
            var planetBatch = new List<Planet>();
            
            // Note: _synchronizedPlanets is cleared and populated with the space view.
            // This will wait an extra frame to allow _synchronizedPlanets to catch up for the initial call.
            Engine
                .NextFrame.Nop()
                .AddTag("SynchronizePlanets")
                .Next.Loop(period: _gameplayConfig.PlanetUpdatePeriod, maxCycles: float.MaxValue, () =>
                {
                    if (SelectedPlanet is not null && planetSet.Add(SelectedPlanet))
                        planetQueue.Enqueue(SelectedPlanet);
                    
                    foreach (Planet planet in _planetsOnScreen)
                    {
                        if (planet.IsVisible && planetSet.Add(planet))
                            planetQueue.Enqueue(planet);
                    }
                    
                    _player.ForEachPlanet(planet =>
                    {
                        if (planetSet.Add(planet))
                            planetQueue.Enqueue(planet);
                    });
                    
                    planetBatch.Clear();

                    for (int i = 0; (i < _gameplayConfig.PlanetUpdateMaxBatchSize) && (planetQueue.Count > 0); ++i)
                    {
                        Planet planet = planetQueue.Dequeue();
                        planetSet.Remove(planet);
                        planetBatch.Add(planet);
                    }

                    if (planetBatch.Count == 0)
                        return;
                    
                    //Debug.Log($"PlanetsUpdateRequested - Batch Size: {planetBatch.Count}");
                    _eventManager.GameplayEvents.PlanetsUpdateRequested?.Invoke(planetBatch);
                });
            
            bool isFirstSync = true; //flag to prevent notification from getting triggered from all the planets "reconquering" when player plays an existing progress
            
            _eventManager.GameplayEvents.PlanetUpdateReceived += planetsData =>
            {
                foreach (PlanetData data in planetsData)
                {
                    if (string.IsNullOrWhiteSpace(data.LocationHash))
                    {
                        Debug.LogWarning($"{data.LocationHash} is invalid.");
                        continue;
                    }

                    if (!_map.TryHashToPlanet(data.LocationHash, out Planet planet))
                    {
                        Debug.LogWarning($"Sector unknown. LocationHash: {data.LocationHash}");
                        continue;
                    }

                    if (planet is null)
                    {
                        Debug.LogError($"A planet should never be null in this situation. LocationHash: {data.LocationHash}");
                        continue;
                    }
                    
                    //change ownership when the owner of a planet in local data is different than the one in payload
                    if (!string.IsNullOrWhiteSpace(data.OwnerPersonaTag) && planet.Owner.PersonaTag != data.OwnerPersonaTag)
                    {
                        Player prevOwner = planet.Owner;

                        if (!planet.Owner.IsAlien) //disclaim from the old owner
                            planet.Owner.Disclaim(planet);

                        if (data.OwnerPersonaTag == _player.PersonaTag) //ownership changed to local player
                        {
                            _player.Claim(planet);
                        }
                        else //ownership changes to other player
                        {
                            if (!_playersByPersonaTag.TryGetValue(data.OwnerPersonaTag, out Player otherPlayer))
                            {
                                otherPlayer = new Player { PersonaTag = data.OwnerPersonaTag };
                                _ = _playerColorManager.TryAssignColor(otherPlayer);
                                _playersByPersonaTag[data.OwnerPersonaTag] = otherPlayer;
                            }

                            otherPlayer.Claim(planet);
                        }

                        //when a planet ownership is changed, export ship is reset, and recheck friendly/hostile incoming transfer
                        planet.ExportShipRemaining = planet.MaxExportShipCount;
                        _eventManager.GameplayEvents.PlanetExportUpdated?.Invoke(planet);
                        _eventManager.GameplayEvents.PlanetIncomingTransferUpdated?.Invoke(planet);
                        _eventManager.GameplayEvents.PlanetControlChanged?.Invoke(planet, planet.Owner, prevOwner);
                    }
                    
                    if (planet.TrySetEnergyLevel(data.EnergyLevel, data.WorldTick, _worldClock.TickToUtc(data.WorldTick)))
                        _eventManager.GameplayEvents.PlanetUpdated?.Invoke(planet);

                    // Note: Renamed because the name was hiding another variable.
                    foreach (EnergyTransfer incomingTransfer in data.energyTransfers)
                    {
                        if (_clientEnergyTransferIds.Contains(incomingTransfer.TransferId))
                            continue;
                        
                        ulong id = incomingTransfer.TransferId;

                        //transfer with locally unexplored source is okay, and will be drawn as a shrinking ring
                        _map.TryHashToPlanet(incomingTransfer.SourceHash, out Planet source);
                        
                        //energy transfer with undiscovered destination do not need to be drawn at all
                        if (!_map.TryHashToPlanet(incomingTransfer.DestinationHash, out Planet destination))
                        {
                            Debug.Log($"Will not display transfer {id} because the destination sector is locally unexplored.");
                            continue;
                        }

                        Vector2Int sourceGridPosition;
                        float travelDuration;

                        if (source is not null)
                        {
                            sourceGridPosition = source.Position;
                            travelDuration = incomingTransfer.TravelTimeInSeconds;
                        }
                        else
                        {
                            //fake source position for transfer with null source, 8 is the set size for the shrinking ring
                            sourceGridPosition = new Vector2Int(destination.Position.x + 8, destination.Position.y);
                            travelDuration = incomingTransfer.TravelTimeInSeconds;
                        }

                        int energyOnEmbark = Mathf.RoundToInt(incomingTransfer.EnergyOnEmbark);
                        float progress = incomingTransfer.Progress;
                        Player shipOwner;
                        
                        if (incomingTransfer.OwnerPersonaTag == _player.PersonaTag)
                        {
                            shipOwner = _player;
                        }
                        else if (!_playersByPersonaTag.TryGetValue(incomingTransfer.OwnerPersonaTag, out shipOwner))
                        {
                            shipOwner = new Player { PersonaTag = incomingTransfer.OwnerPersonaTag };
                            _ = _playerColorManager.TryAssignColor(shipOwner);
                            _playersByPersonaTag[data.OwnerPersonaTag] = shipOwner;
                        }
                        
                        var et = new ClientEnergyTransfer
                        {
                            Id = incomingTransfer.TransferId,
                            Source = source,
                            Destination = destination,
                            TravelDuration = travelDuration,
                            EnergyOnEmbark = energyOnEmbark,
                            Progress = progress,
                            ShipOwner = shipOwner
                        };
                        
                        // Please note: We could be sending a null source planet and that's fine. It's in a sector that hasn't been discovered by the local player.
                        StartEnergyTransfer(et, transferStartScreenPosition: _spaceCamera.WorldToScreenPoint((Vector2)sourceGridPosition));
                    }
                }

                if (isFirstSync)
                {
                    isFirstSync = false;
                    _eventManager.GameplayEvents.FirstUpdateDone?.Invoke();
                }
            };
        }
        
        private void InitializeLeaderboard()
        {
            // Set this on the next frame so that everything is setup properly.
            Engine.NextFrame
                .Add(() => _eventManager.GameplayEvents.InstanceNameUpdated?.Invoke(_worldInstanceInfo.Name))
                .Next.Log($"Instance name is: '{_worldInstanceInfo.Name}'");
            
            // Player leaderboard stat periodic update /////////////////////////
            
            Engine
                .NextFrame.Nop().AddTag("PlayerLeaderStatUpdate")
                .Next.Loop(period: 5f, maxCycles: float.MaxValue, () =>
                {
                    if (string.IsNullOrEmpty(_player.PersonaTag))
                    {
                        Debug.LogError("_player.PersonaTag is null or empty.");
                        return;
                    }

                    _eventManager.GameplayEvents.PlayerLeaderStatUpdateRequested?.Invoke(_player.PersonaTag);
                });

            // Leaderboard refresh (on update and every 5s) ////////////////////
            
            Engine
                .NextFrame.Nop().AddTag(LeaderboardTag)
                .Next.Loop(period: 5f, maxCycles: float.MaxValue, () =>
                {
                    _eventManager.GameplayEvents.LeaderboardUpdateRequested?.Invoke(_player.PersonaTag);
                });
            
            Engine.Pause(LeaderboardTag);

            _eventManager.GameplayEvents.LeaderboardOpened += () => Engine.Resume(LeaderboardTag);
            _eventManager.GameplayEvents.LeaderboardClosed += () => Engine.Pause(LeaderboardTag);
        }
        
        //zoomScale use planet scale
        private void Focus(Vector3 position, float zoomScale)
        {
            position.z = SpaceCameraTransform.position.z;
            SpaceCameraTransform.position = position;
            _spaceCamera.orthographicSize = 5 + (zoomScale * 10);
            _eventManager.GameplayEvents.CameraZoomChanged?.Invoke(_spaceCamera.orthographicSize);
        }
        
        private void FocusOnPlanet(Planet focusedPlanet)
        {
            Vector3 focusedPosition = (Vector2)focusedPlanet.Position;
            focusedPosition.z = SpaceCameraTransform.position.z;
            Focus(focusedPosition, focusedPlanet.Scale);
        }
        
        private void PanSpaceCamera(Vector2 offset) => SpaceCameraTransform.Translate(offset);

        private void AlignView()
        {
            Vector3 originalPosition = SpaceCameraTransform.position;
            Vector2 p = originalPosition.XY();

            if (p.sqrMagnitude <= _map.WorldRadius * _map.WorldRadius)
                return;
            
            p = p.normalized * _map.WorldRadius;
            var destination = new Vector3(p.x, p.y, originalPosition.z);
            SpaceCameraTransform.position = Vector3.Lerp(originalPosition, destination, .5f * saturate(Time.deltaTime * 10f));
        }

        private void UpdatePlanets()
        {
            _planetsOnScreen.Clear();

            // These 2 constants could probably be a bit more accessible.
            const int maxVisiblePlanetsTarget = 256;
            const int maxVisibleLod = 5;

            float zoom = _spaceCamera.orthographicSize;
            int minPlanetLevel = max(0, Mathf.CeilToInt(-9f + Mathf.Log(zoom, 1.618f)));

            // Display world radius.
            _overlay.DrawWorldRing(Vector3.zero, _map.WorldRadius, Color.red);

            Bounds cameraWorldBounds = SpaceCameraWorldBounds;
            float screenScale = ScreenScale;

            _hitTestResultPlanet = null;

            _map.Quadtree.WalkBfs(node =>
            {
                if (!node.Sector.HasPlanet)
                    return false;

                Planet planet = node.Sector.Planet;
                bool keep = _player.IsHomePlanet(planet) || (planet == SelectedPlanet);

                // Since this isn't an LodScale value, 0 is the highest level.
                planet.Lod = 0;

                if (!keep)
                {
                    float distanceFromVisibilitySquared = cameraWorldBounds.SqrDistance(new Vector2(planet.Position.x, planet.Position.y));
                    float fullRange = max(planet.Radius, planet.FullRange);

                    // If there's no chance of showing anything of this planet, then no need to continue processing this planet, but we do have to consider those below.
                    // Note: This could be a very large planet located on the far side of a quadrant that intersects camera bounds.
                    // Child quadrants might still contain viable planets.
                    if (distanceFromVisibilitySquared >= fullRange * fullRange)
                        return true;

                    if (SelectedPlanet is not null)
                    {
                        float distanceToSelectedPlanetSquared = (SelectedPlanet.Position - planet.Position).sqrMagnitude;
                        float selectedPlanetFullRange = SelectedPlanet.FullRange; // Maybe add a small amount of padding.

                        // Keep planets longer than they otherwise would be if they're within range.
                        if (distanceToSelectedPlanetSquared <= selectedPlanetFullRange * selectedPlanetFullRange)
                        {
                            keep = true;
                            planet.Lod -= 10;
                        }
                    }

                    if (_planetsOnScreen.Count > maxVisiblePlanetsTarget)
                    {
                        // off-center weight
                        int offCenterScale = 3;
                        Vector2 np = Rect.PointToNormalized(new Rect(cameraWorldBounds.min.XY(), cameraWorldBounds.size.XY()), new Vector2(planet.Position.x, planet.Position.y));
                        np -= .5f * Vector2.one;
                        np *= 2f;
                        np *= np;
                        np *= offCenterScale;

                        planet.Lod += (int)max(np.x, np.y);
                    }

                    if (planet.Level >= minPlanetLevel)
                    {
                        keep = true;

                        if (_planetsOnScreen.Count > maxVisiblePlanetsTarget)
                            planet.Lod += (_planetsOnScreen.Count / maxVisiblePlanetsTarget);
                    }
                    else
                    {
                        planet.Lod += minPlanetLevel - planet.Level;

                        if (planet.Lod <= maxVisibleLod)
                            keep = true;
                    }
                }

                planet.Lod = max(0, planet.Lod);

                if (!keep)
                    return true;

                float extraScale = 1f; // Use this value increase planet scale for testing purposes.
                float localPlanetScale = planet.Scale * extraScale;
                float hitTestScale = extraScale * .5f + screenScale;

                RenderBatcher renderBatcher = _renderBatchers[planet.Level];
                planet.Depth = 100f + planet.Level * 100f + abs(planet.Position.x ^ planet.Position.y) % 99;
                var p = new Vector3(planet.Position.x, planet.Position.y, planet.Depth);
                Matrix4x4 worldMatrix = Matrix4x4.Translate(p);
                worldMatrix *= Matrix4x4.Scale(Vector3.one * localPlanetScale);

                float lodScale = remap(1f, maxVisibleLod + 1f, 1f, 0f, planet.Lod);
                Color.RGBToHSV(planet.Owner.Color, out float h, out float s, out float _);
                Color boostedOwnerColor = Color.HSVToRGB(h, s, 1f).WithAlpha(lodScale);

                if (node.PlanetIntersects(cameraWorldBounds))
                {
                    if (planet.Lod == 0)
                    {
                        renderBatcher.Add(worldMatrix);

                        //last time planet is visible is before the last frame, so this planet just showed in this frame
                        if (planet.LastVisibleFrameIndex < (Time.frameCount - 1))
                            _eventManager.GameplayEvents.PlanetShown?.Invoke(planet);

                        planet.LastVisibleFrameIndex = Time.frameCount;
                    }
                    else if (planet.Lod <= maxVisibleLod)
                    {
                        _overlay.DrawWorldDotWithSsr(p.XY(), lodScale * (planet.Level + 6f), boostedOwnerColor);
                    }

                    //planet was visible in previous frame, on the screen but is not in lod 0 now, triggers hidden
                    if (planet.Lod > 0 && planet.LastVisibleFrameIndex == Time.frameCount - 1)
                        _eventManager.GameplayEvents.PlanetHidden?.Invoke(planet);

                    _planetsOnScreen.Add(planet);
                }
                else
                {
                    //planet was visible in previous frame, but not on the screen now, triggers hidden
                    if (planet.LastVisibleFrameIndex == Time.frameCount - 1)
                        _eventManager.GameplayEvents.PlanetHidden?.Invoke(planet);
                }

                _eventManager.GameplayEvents.PlanetUpdated?.Invoke(planet);

                if (node.HitTestPlanet(MouseWorldPosition, hitTestScale))
                {
                    // Adding radius and scale because we could alter some of these things in the future.
                    if (_hitTestResultPlanet is null || planet.Depth + planet.Radius * localPlanetScale < _hitTestResultPlanet.Depth + _hitTestResultPlanet.Radius * localPlanetScale)
                    {
                        _hitTestResultPlanet = planet;
                        _hitTestPosition = planet.Position;
                    }
                }

                // If we don't need to display the extra detail, move on.
                if (planet.Lod > 0)
                    return true;

                float ringRadius = planet.Scale * 0.65f + _spaceCamera.orthographicSize * .01f;

                if (planet == SelectedPlanet)
                {
                    // Current Range
                    if (_player.Owns(planet))
                    {
                        _overlay.DrawWorldRing(p, (float)planet.GetCurrentRange(_energyTransferScale),
                            ColorPalette.LocalRangeRing, _gameplayConfig.LocallyOwnedRangeRingStrokeWidth);
                    }

                    //player or enemy
                    if (planet.IsClaimed)
                    {
                        Color ringColor = _player.Owns(planet)
                            ? ColorPalette.LocalRangeRing.WithAlpha(.4f)
                            : ColorPalette.Enemy.WithAlpha(.4f);

                        //full range
                        _overlay.DrawWorldRing(p, planet.FullRange, ringColor,
                            _gameplayConfig.SelectedPlanetStrokeWidth, _gameplayConfig.SelectedPlanetMinSegments);
                    }
                    else
                    {
                        //full range
                        _overlay.DrawWorldRing(p, planet.FullRange, ColorPalette.UnclaimedRing.WithAlpha(.4f),
                            _gameplayConfig.SelectedPlanetStrokeWidth, _gameplayConfig.SelectedPlanetMinSegments);
                    }

                    //selection ring
                    _overlay.DrawWorldRing(p, ringRadius, ColorPalette.SelectionRing, 3f);
                }
                else //energy ring
                {
                    if (planet.IsClaimed)
                    {
                        Color energyRingColor = planet.Owner.Color.WithAlpha(0.6f);
                        _overlay.DrawWorldRing(p, ringRadius, energyRingColor, _gameplayConfig.EnergyRingThinStrokeWidth, -(1f - (float)(planet.EnergyLevel / planet.EnergyCapacity)));
                        _overlay.DrawWorldRing(p, ringRadius, energyRingColor, _gameplayConfig.EnergyRingThickStrokeWidth, (float)(planet.EnergyLevel / planet.EnergyCapacity));
                    }
                }

                //selection ring on hover
                if (planet == _targetedPlanet)
                {
                    _overlay.DrawWorldRing(p, ringRadius, ColorPalette.SelectionRing, 3f);
                }

                return true;
            });

            foreach (RenderBatcher renderBatcher in _renderBatchers)
            {
                renderBatcher.Render(cameraWorldBounds);
                renderBatcher.Clear();
            }

            if (_debugMode == DebugMode.Inactive)
                return;
            
            StringBuilder sb = GenericPool<StringBuilder>.Get();
            const int fontSize = 15;
            const int lineHeight = fontSize + 1;
            int labelIndex = 0;
            int maxRows = (Screen.height - 300) / lineHeight;

            switch (_debugMode)
            {
                case DebugMode.BasicWithPlanets:
                case DebugMode.ExtraWithPlanets:
                {
                    foreach (Planet planet in _planetsOnScreen)
                    {
                        sb.Clear();
                        sb.Append(planet.Lod);
                        sb.Append(" ");
                        sb.Append(planet.Level);
                        sb.Append(" ");
                        sb.Append(planet.Name);
                        sb.Append(" ");
                        sb.Append(planet.Position.x);
                        sb.Append(",");
                        sb.Append(planet.Position.y);
                        sb.AppendLine();

                        var textColor = new Color(1f, .85f, .25f);

                        if (planet == SelectedPlanet)
                            textColor = Color.cyan;
                        else if (planet == _targetedPlanet)
                            textColor = Color.white;
                        else if (_player.Owns(planet))
                            textColor = Color.green;
                        else if (planet.IsClaimed)
                            textColor = Color.red;

                        int rowIndex = (labelIndex % maxRows);

                        if (rowIndex % 2 == 1)
                            textColor = (textColor * .85f).WithAlpha(1f);

                        _overlay.DrawImGuiText(new Vector2(
                            Screen.width - 224 * (1 + floor(labelIndex++ / (float)maxRows)),
                            Screen.height - 160 - (rowIndex * lineHeight)),
                            fontSize, textColor, sb.ToString(), true, false);
                    }

                    break;
                }
            }
        }

        private void OnSendEnergyTransfer(SendEnergyReceipt receipt)
        {
            if (receipt.Ship is null)
            {
                Debug.LogError("SendEnergyApproved: receipt.Ship is null! Skipping.");
                return;
            }
            
            if (string.IsNullOrEmpty(receipt.Ship.LocationHashFrom))
            {
                Debug.LogError($"AddNewEnergyTransfer: receipt.Ship.locationHashFrom: {receipt.Ship.LocationHashFrom}\n{string.Join('\n', receipt.Errors)}");
                return;
            }
            
            if (string.IsNullOrEmpty(receipt.Ship.LocationHashTo))
            {
                Debug.LogError($"AddNewEnergyTransfer: receipt.Ship.locationHashTo: {receipt.Ship.LocationHashTo}\n{string.Join('\n', receipt.Errors)}");
                return;
            }
            
            Debug.Log($"{nameof(_eventManager.GameplayEvents.SendEnergyApproved)}: src: {receipt.Ship.LocationHashFrom.Take(5)} dst: {receipt.Ship.LocationHashTo.Take(5)}");
            
            if (!_map.TryHashToPlanet(receipt.Ship.LocationHashFrom, out Planet source))
            {
                Debug.LogError($"Couldn't map {nameof(receipt.Ship.LocationHashFrom)}:{receipt.Ship.LocationHashFrom} to planet.");
                return;
            }
            
            if (!_map.TryHashToPlanet(receipt.Ship.LocationHashTo, out Planet destination))
            {
                Debug.LogError($"Couldn't map {nameof(receipt.Ship.LocationHashTo)}:{receipt.Ship.LocationHashTo} to planet.");
                return;
            }
            
            // Removing as early as possible to ensure that this information won't be lost.
            // As a precaution, the routine in question will eventually timeout too.
            Engine.Remove(source.PendingEnergyTransferTag);
            
            //check first if it's already exist in ongoingtransfer (it's possible that there is a state update that already contains the transfer before the receipt is received)
            if (_clientEnergyTransferIds.Contains(receipt.Ship.Id))
                return;
            
            var et = new ClientEnergyTransfer
            {
                Id = receipt.Ship.Id,
                Source = source,
                Destination = destination,
                TravelDuration = (float)_worldClock.TicksToSeconds(receipt.Ship.DurationInTicks),
                EnergyOnEmbark = Mathf.RoundToInt((float)receipt.Ship.EnergyOnEmbark),
                ShipOwner = _player
            };
            
            Debug.Log($"From receipt: SourceEnergyRemaining: {receipt.SourceEnergyRemaining}");
            
            _ = et.Source.TrySetEnergyLevel(receipt.SourceEnergyRemaining, receipt.Ship.TickStart, _worldClock.TickToUtc(receipt.Ship.TickStart));
            
            StartEnergyTransfer(et, transferStartScreenPosition: _spaceCamera.WorldToScreenPoint((Vector2)et.Source.Position));
        }
        
        private void StartEnergyTransfer(ClientEnergyTransfer transfer, Vector3 transferStartScreenPosition)
        {
            if (!_clientEnergyTransferIds.Add(transfer.Id))
                return;
            
            Engine.NextFrame.Add(new EnergyTransferRoutine(transfer,
                enterCallback: () =>
                {
                    if (transfer.Source is not null && transfer.Source.Owner == transfer.ShipOwner)
                    {
                        transfer.Source.AddOutboundTransfer(transfer);
                        _eventManager.GameplayEvents.PlanetExportUpdated?.Invoke(transfer.Source);
                    }
                    
                    transfer.Destination.AddInboundTransfer(transfer);
                    _eventManager.GameplayEvents.EnergyTransferStarted?.Invoke(transfer, transferStartScreenPosition, UIUtility.GetTransferInfoArmRotation(transfer.TransferDirectionVector));
                    _eventManager.GameplayEvents.PlanetIncomingTransferUpdated?.Invoke(transfer.Destination);
                },
                updateCallback: () =>
                {
                    Vector2 wa = transfer.Source?.WorldPosition ?? transfer.Destination.WorldPosition + new Vector2(8f,0);
                    Vector2 wb = transfer.Destination.WorldPosition;
                    Color c = transfer.LineColor;

                    //null source planet means it's from an undiscovered sector
                    if (transfer.Source is not null)
                    {
                        _overlay.DrawWorldLine(wa, wb, c, _gameplayConfig.TransferLineDashScale, _gameplayConfig.TransferLineGapToDashRatio);

                        if (transfer.Progress <= 0f)
                            return;
                        
                        float2 wp = lerp(wa, wb, transfer.Progress);
                        // The depth math prevents collisions.
                        Matrix4x4 worldMatrix = Matrix4x4.Translate(new Vector3(wp.x, wp.y, -10f - 4f * (transfer.Id % 15f)));
                        worldMatrix *= Matrix4x4.Rotate(Quaternion.Euler(new Vector3(-90f, 0f, 0f)));
                        worldMatrix *= Matrix4x4.Rotate(Quaternion.Euler(new Vector3(0f, (Mathf.Rad2Deg * Mathf.Atan2(wb.y - wa.y, wb.x - wa.x) * -1f) + 90f, 0f)));
                        worldMatrix *= Matrix4x4.Scale((1f + transfer.Source.Level * .5f) * Vector3.one);
                        
                        foreach (MeshMaterialPair mesh in _transportShipConfig.Meshes)
                            Graphics.RenderMesh(new RenderParams(mesh.material), mesh.mesh, 0, worldMatrix);

                        _eventManager.GameplayEvents.EnergyTransferUpdated?.Invoke(transfer.Id,
                            _spaceCamera.WorldToScreenPoint(new Vector3(wp.x, wp.y, 0)),
                            TimeSpan.FromSeconds(transfer.DurationRemaining));
                    }
                    else
                    {
                        //draw shrinking ring if transfer is from unexplored grid
                        float2 wp = lerp(wa, wb, transfer.Progress);
                        float dist = distance(wp, wb);
                        
                        _overlay.DrawWorldRing(new Vector3(wb.x, wb.y, 0f), dist, transfer.LineColor, 2f);
                        
                        _eventManager.GameplayEvents.EnergyTransferUpdated?.Invoke(transfer.Id,
                            _spaceCamera.WorldToScreenPoint(new Vector3(wb.x + dist, wb.y, 0)),
                            TimeSpan.FromSeconds(transfer.DurationRemaining));
                    }
                },
                exitCallback: () =>
                {
                    _eventManager.GameplayEvents.EnergyTransferArrived?.Invoke(transfer);
                        
                    if (transfer.Source is not null && transfer.Source.Owner == transfer.ShipOwner)
                    {
                        transfer.Source.RemoveOutboundTransfer(transfer.Id);
                        _eventManager.GameplayEvents.PlanetExportUpdated?.Invoke(transfer.Source);
                    }
                        
                    transfer.Destination.RemoveInboundTransfer(transfer.Id); 
                    _eventManager.GameplayEvents.PlanetIncomingTransferUpdated?.Invoke(transfer.Destination);
                    _ = _clientEnergyTransferIds.Remove(transfer.Id);
                }));
        }
        
        private void InitializeDebugView(out Action<string> displayStateInfo, out Action monitorDebugTextToggles)
        {
            // Display the most recent error message (if any) and give it at least 1 second to remain on screen, or longer if no others are queued.
            _eventManager.GeneralEvents.ErrorEncountered += (errorMessage, _)
                => _errorMessageQueue.Enqueue($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture)}: {errorMessage}");
            
            var errorMessageUpdater = Engine.NextFrame.Loop(period: 4f, maxCycles: float.MaxValue,
                () => _mostRecentErrorMessage = (_errorMessageQueue.Count > 0) ? _errorMessageQueue.Dequeue() : string.Empty);

            Engine.NextFrame.Loop(() =>
            {
                if (_errorMessageQueue.Count > 0)
                    errorMessageUpdater.Speed = pow(_errorMessageQueue.Count, 2);

                if (!string.IsNullOrEmpty(_mostRecentErrorMessage))
                    _overlay.DrawImGuiText(new Vector2(Screen.width * .15f, Screen.height - 28),
                    12, Color.red.WithAlpha(10f * Easing.Sin01((float)errorMessageUpdater.Position)), _mostRecentErrorMessage, isDebugText: true);
            }).AddTag("ErrorMessage");
            
            // Display game state info.
            
            float fps = 0;
            float minFps = float.MaxValue;
            float maxFps = float.MinValue;
            var stateInfoStringBuilder = new StringBuilder();
            var routines = new List<CyclopsRoutine>();
            
            void DisplayStateInfo(string stateName)
            {
                if (_debugMode == DebugMode.Inactive)
                    return;
                
                stateInfoStringBuilder.Clear();
                stateInfoStringBuilder.Append(stateName);

                void AppendPlanetInfo(Planet planet)
                {
                    if (planet is null)
                        return;

                    int pendingEnergyTransferCount = Engine.Count(planet.PendingEnergyTransferTag);
                    
                    stateInfoStringBuilder.Append((planet == SelectedPlanet) ? "  //  Selection: " : "  //  Target: ");
                    stateInfoStringBuilder.Append(planet.Owner.PersonaTag);
                    stateInfoStringBuilder.Append(" owns ");
                    stateInfoStringBuilder.Append(planet.Name);
                    
                    if (pendingEnergyTransferCount > 0)
                        stateInfoStringBuilder.Append($" // {planet.PendingEnergyTransferTag}: {pendingEnergyTransferCount}");
                    
                    stateInfoStringBuilder.Append(" // Tick: ");
                    stateInfoStringBuilder.Append(planet.LastWorldTick);
                }

                AppendPlanetInfo(SelectedPlanet);
                AppendPlanetInfo(_targetedPlanet);
                
                _overlay.DrawImGuiText(Vector2.one * 8, 12, Color.yellow, stateInfoStringBuilder.ToString(), isDebugText:true);
                
                float rawFps = Engine.Fps;
                
                fps = lerp(fps, rawFps, .02f);
                minFps = lerp(min(rawFps, minFps), fps, .01f);
                maxFps = lerp(max(rawFps, maxFps), fps, .01f);
                
                stateInfoStringBuilder.Clear();
                stateInfoStringBuilder.Append("FPS // Min: ");
                stateInfoStringBuilder.Append(round(minFps).ToString("000"));
                stateInfoStringBuilder.Append(" // Avg: ");
                stateInfoStringBuilder.Append(round(fps).ToString("000"));
                stateInfoStringBuilder.Append(" // Max: ");
                stateInfoStringBuilder.Append(round(maxFps).ToString("000"));
                
                _overlay.DrawImGuiText(new Vector2(min((int)(Screen.width * .618), Screen.width - 512), Screen.height - 28),
                    12, Color.yellow, stateInfoStringBuilder.ToString(), isDebugText:true);

                if (_debugMode is not (DebugMode.Extra or DebugMode.ExtraWithPlanets))
                    return;
                
                Engine.CopyRoutinesToList(routines);

                int rowIndex = 0;

                foreach (CyclopsRoutine routine in routines)
                {
                    int progress = Mathf.RoundToInt((float)routine.Position * 10f);

                    stateInfoStringBuilder.Clear();

                    if (routine.Period is > 0 and < float.MaxValue)
                    {
                        stateInfoStringBuilder.Append('*', progress);
                        stateInfoStringBuilder.Append(' ', 10 - progress);
                    }
                    else
                    {
                        stateInfoStringBuilder.Append('*', 10);
                    }

                    stateInfoStringBuilder.Append(" | ");
                    stateInfoStringBuilder.Append(routine.Age >= int.MaxValue ? "      MAX " : $"{routine.Age,10:0.00}");
                    stateInfoStringBuilder.Append(" of ");
                    stateInfoStringBuilder.Append(routine.MaxCycles >= int.MaxValue ? "    MAX " : $"{routine.MaxCycles,8:0.00}");
                    stateInfoStringBuilder.Append(" | ");
                    stateInfoStringBuilder.Append(routine.IsPaused ? "Paused" : "Active");
                    stateInfoStringBuilder.Append(" | ");
                    stateInfoStringBuilder.Append(routine.GetType().Name.PadRight(22));

                    if (routine.Tags.Any())
                    {
                        stateInfoStringBuilder.Append(" | Tags: ");

                        foreach (string tag in routine.Tags)
                        {
                            stateInfoStringBuilder.Append("'");
                            stateInfoStringBuilder.Append(tag);
                            stateInfoStringBuilder.Append("' ");
                        }
                    }

                    if (routine is EnergyTransferRoutine etr)
                    {
                        ClientEnergyTransfer et = etr.Transfer;

                        stateInfoStringBuilder.Append(" | Info: ");
                        stateInfoStringBuilder.Append(et.Source is not null ? et.Source.Name : "Unknown");
                        stateInfoStringBuilder.Append(" -> ");
                        stateInfoStringBuilder.Append(et.Destination.Name);
                        stateInfoStringBuilder.Append(" Energy: ");
                        stateInfoStringBuilder.Append(et.EnergyOnEmbark);
                        stateInfoStringBuilder.Append(" TxId: ");
                        stateInfoStringBuilder.Append(et.Id);
                        stateInfoStringBuilder.Append(" Sender: ");
                        stateInfoStringBuilder.Append(et.ShipOwner.PersonaTag);
                        stateInfoStringBuilder.AppendLine();
                    }

                    _overlay.DrawImGuiText(new Vector2((Screen.width >> 1) - 432, Screen.height - 128 - rowIndex++ * 16),
                        16, new Color(.75f, .75f, .75f), stateInfoStringBuilder.ToString(), isDebugText: true, false);
                }
            }

            void MonitorDebugTextToggles()
            {
                // Unified hack.
                if (!_gameplayInput.ToggleSpreadsheet.WasPressedThisFrame() && !_gameplayInput.ToggleDebugInfo.WasPressedThisFrame())
                    return;
                
                _debugMode = (DebugMode)(((int)_debugMode + 1) % 5);
                _overlay.IsDebugTextEnabled = _debugMode is not DebugMode.Inactive;
            }
            
            displayStateInfo = DisplayStateInfo;
            monitorDebugTextToggles = MonitorDebugTextToggles;
        }
    }
}