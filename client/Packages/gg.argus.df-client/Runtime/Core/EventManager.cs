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
using ArgusLabs.DF.Core.Communications;
using ArgusLabs.DF.Core.Configs;
using ArgusLabs.DF.Core.Space;
using ArgusLabs.WorldEngineClient.Communications.Rpc;
using UnityEngine;

// WARNING: Please be extra careful regarding tense when interpreting what these "events" actually do.
// Although you may be expecting events to tell you what is happen-ing or what just happen-ed,
// some of these events are setup as though they know about things outside their publisher's scope.
// Unless you see a communications event with a Requested/Received suffix, please be sure to check both sides to make sure you're not missing anything.
// We chose not to use Singletons in an effort to pause and think about what we're doing and how to maintain proper scope and separation of concerns,
// but things happen and some of these could easily be Singleton workarounds.

namespace ArgusLabs.DF.Core
{
    public class EventManager
    {
        public GeneralEvents GeneralEvents { get; private set; }
        public MainMenuEvents MainMenuEvents { get; private set; }
        public GameplayEvents GameplayEvents { get; private set; }

        public EventManager()
        {
            GeneralEvents = new GeneralEvents();
            MainMenuEvents = new MainMenuEvents();
            GameplayEvents= new GameplayEvents();
        }
    }

    public enum GameError
    {
        Undefined,
        Communications,
        Gameplay
    }
    
    public class GeneralEvents
    {
        public delegate void ErrorNotificationDelegate(string errorMessage, GameError id = GameError.Undefined);
        public ErrorNotificationDelegate ErrorEncountered;
        
        public Action Disconnected;

        public Action RetryConnection;

        public Action OpenTutorial;
        public Action OpenControls;
        public Action OpenLinkedEvmList;
        public Action OpenLinkNewEvm;

        public Action<string> LinkNewEvmAddress;
        public Action<bool> LinkNewEvmAddressCompleted;
        public Action RequestLinkedEvmList;
        public Action<string[]> RespondLinkedEvmList;

        public Action InGameSubmenuClosed;

        public Action<string> MessagePopupOpen;

        public Action MessagePopupClose;

        // I added this delegate to allow the callback to be optional.
        // It seems like it's frequently null.
        public delegate void PopupDelegate(string message, Action callback = null);
        public PopupDelegate OneButtonPopup;

        public delegate void WinnerPopupDelegate(Player player, Action callback = null);
        public WinnerPopupDelegate WinnerPopup;

        // bool: show "finding home planet" message or not
        public Action<bool> LoadingStarted;

        public Action LoadingFinished;

        public Action<Sector> SectorChanged;
        public Action<Sector> SectorExplored;

        public Action ScreenSizeChanged;

        public Action RequestPlayerTag;
        public Action<string> RespondPlayerTag;
    }

    public class MainMenuEvents
    {
        /// <summary>
        /// Event to open splash screen from the backend
        /// </summary>
        public Action SplashScreenOpen;

        /// <summary>
        /// Event to close splash screen and the whole pre game sequence
        /// </summary>
        public Action SplashScreenClose;
        
        public Action KeyAndTagScreenOpen;

        public Action BetaKeyEntryOpen;

        public Action<string> BetaKeySubmitted;

        public Action BetaKeyEntryClose;

        public Action PlayerTagEntryOpen;

        public Action<string> PlayerTagSubmitted;

        public Action PlayerTagEntryClose;

        public Action LinkStartOpen;

        public Action<string> KeyAndTagMessageOpen;

        public Action KeyAndTagMessageClose;

        public Action KeyAndTagScreenClose;

        /// <summary>
        /// Event that is called when splashscreen animation is finished and loading should be started
        /// </summary>
        public Action SplashScreenAnimationFinished;
    }

    public class GameplayEvents
    {
        public Action GameplayEntered;
        public Action GameplayExited;
        
        // /// <summary>
        // /// Event to open in-game UI from the backend
        // /// </summary>
        // public Action GameplayUIOpen;

        /// <summary>
        /// Event for when explorer is changed into moving or stopped from the backend, bool = true if explorer is set to move, false if set to stopped
        /// </summary>
        public Action<bool> ExplorerMoveStateChanged;

        /// <summary>
        /// Event for when explorer is set into start from UI
        /// </summary>
        public Action ExplorerStarting;

        /// <summary>
        /// Event for when explorer is set into stop from UI
        /// </summary>
        public Action ExplorerStopping;

        /// <summary>
        /// Event for when player change the state of the game into relocating explorer (from UI button)
        /// </summary>
        public Action ExplorerStartRelocating;
        
        /// <summary>
        /// Event for when player cancelled the explorer relocation (by pressing UI button)
        /// </summary>
        public Action ExplorerInvalidRelocationAttempted;

        /// <summary>
        /// Event for when player cancelled the explorer relocation (by pressing UI button)
        /// </summary>
        public Action ExplorerStopRelocating;

        /// <summary>
        /// Event for when the state of the game is entering or exiting relocating state, set from backend, bool = true if entering relocating state, false if exiting relocating state
        /// </summary>
        public Action<bool> RelocatingStateChanged;
        
        /// <summary>
        /// Invoke when the player's home planet is claimed.
        /// </summary>
        public Action<Vector2Int, ClaimHomePlanetMsg> HomePlanetClaimRequested;
        
        /// <summary>
        /// Invoke when we want to send extra energy to a specific planet for debugging purposes.
        /// </summary>
        public Action<LocationHashMsg> DebugEnergyBoosted;
        
        /// <summary>
        /// Invoke when the player's home planet claim is approved.
        /// </summary>
        public Action HomePlanetClaimApproved;
        
        /// <summary>
        /// Invoke when the player's home planet claim is denied.
        /// </summary>
        public Action HomePlanetClaimDenied;
        
        /// <summary>
        /// Invoke when energy is sent to from one planet to another. 
        /// </summary>
        public Action<Vector2Int, Vector2Int, SendEnergyMsg> SendEnergyRequested;

        public Action<SendEnergyReceipt> SendEnergyApproved;
        
        /// <summary>
        /// Event for when player select a planet via planet index, vector2Int = grid position of the planet
        /// </summary>
        public Action<Vector2Int> PlanetIndexSelectPlanet;

        /// <summary>
        /// Event for when a player take control of a new planet
        /// </summary>
        public delegate void PlanetControlChangedDelegate(Planet takenPlanet, Player newOwner, Player previousOwner);
        public PlanetControlChangedDelegate PlanetControlChanged;

        /// <summary>
        /// Event for when a planet is hovered by player's cursor. Planet = the hovered planet, bool = whether the planet is owned by the local player or not
        /// </summary>
        public Action<Planet, bool> PlanetHoverStarted;

        public Action PlanetHoverEnded;

        /// <summary>
        /// Event for when a planet is selected by player. Planet = the selected planet, bool = whether the planet is owned by the local player or not (so that ui don't need to know player id)
        /// </summary>
        public Action<Planet, bool, SpaceEnvironment> PlanetSelected;

        /// <summary>
        /// Event for when player deselect a planet (and the game state exit from selected state)
        /// </summary>
        public Action PlanetDeselected;

        /// <summary>
        /// string = planetId, int = new energy amount
        /// </summary>
        public Action<Planet> PlanetUpdated;

        /// <summary>
        /// Event for when player changes the percentage amount of energy that they want to transfer, int = percentage of max energy that player wants to transfer (0-100)
        /// </summary>
        public Action<int> EnergyTransferPercentUpdated;

        /// <summary>
        /// Event for when the number of energy export from the selected planet is changed, string = planet id, short 1 = current ship remaining, short 2 = max export ship
        /// </summary>
        public Action<Planet> PlanetExportUpdated;

        /// <summary>
        /// Event for when the number of energy export from the selected planet is changed, planet = the updated planet
        /// </summary>
        public Action<Planet> PlanetIncomingTransferUpdated;

        // spaceviews /////////////////////////////////////

        public Action PlanetInfoClosed;

        /// <summary>
        /// Event for when a planet is just shown on the screen with lod 0 (but not on the previous frame), Planet = the planet shown
        /// </summary>
        public Action<Planet> PlanetShown;

        /// <summary>
        /// Event for when a planet exits screen or its lod is higher than 0 in the latest frame but not on the previous frame, Planet = the planet hidden
        /// </summary>
        public Action<Planet> PlanetHidden;

        public Action<IList<Planet>> PlanetsUpdateRequested;
        public Action<PlanetData[]> PlanetUpdateReceived;
        
        /// <summary>
        /// Event for when player is dragging the cursor to do energy transfer, float = info arm rotation, TimeSpan = transfer duration, int = energy transfered
        /// </summary>
        public Action<float, TimeSpan, int> EnergyTransferPreview;

        public Action<string, bool, int> EnergyTransferTargetHoverStart;

        public Action EnergyTransferTargetHoverStop;

        /// <summary>
        /// Event for when the game state is entering energy transfer
        /// </summary>
        public Action EnergyTransferStateEntered;

        /// <summary>
        /// Event for when the game state is exiting energy transfer
        /// </summary>
        public Action EnergyTransferStateExited;

        public delegate void EnergyTransferStartedDelegate(ClientEnergyTransfer transfer, Vector2 shipPosition, float rotation);
        public EnergyTransferStartedDelegate EnergyTransferStarted;

        /// <summary>
        /// Event for when the energy transfer "ship" is transferring the energy, vector2 = current position of the "ship", TimeSpan = remaining duration of the transfer, int = the energy being transferred
        /// </summary>
        public Action<ulong, Vector2, TimeSpan> EnergyTransferUpdated;

        public Action<ClientEnergyTransfer> EnergyTransferArrived;

        /// <summary>
        /// Event for when player changes the zoom value, either in or out, float = the new orthographicSize value of the camera
        /// </summary>
        public Action<float> CameraZoomChanged;

        /// <summary>
        /// Invoke any time the gameplay view movement (Vector3)delta changes.
        /// </summary>
        //public Action<Vector3> SpaceViewMovementDeltaChanged;

        public Action<Sector> GridHovered;

        public Action<string> PlayerLeaderStatUpdateRequested;

        public Action<PlayerRankReply, string> PlayerLeaderStatUpdated;

        public Action<string> LeaderboardUpdateRequested;

        public Action<LeaderboardReply, string> LeaderboardUpdated;

        public Action LeaderboardOpened;

        public Action LeaderboardClosed;

        public Action<string> InstanceNameUpdated;

        public Action<double> InstanceTimerUpdated;

        public Action FirstUpdateDone;
    }
}
