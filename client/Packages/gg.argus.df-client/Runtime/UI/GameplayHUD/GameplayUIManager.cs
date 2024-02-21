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

using ArgusLabs.DF.Core;
using ArgusLabs.DF.Core.Configs;
using ArgusLabs.DF.Core.Space;
using ArgusLabs.DF.UI.GameplayHUD.InGameMenu;
using ArgusLabs.DF.UI.GameplayHUD.Leaderboard;
using ArgusLabs.DF.UI.GameplayHUD.PlanetIndex;
using ArgusLabs.DF.UI.GameplayHUD.SpaceView.PlanetInfo;
using ArgusLabs.DF.UI.Utilities;
using UnityEngine;

namespace ArgusLabs.DF.UI.GameplayHUD
{
    public class GameplayUIManager : MonoBehaviour
    {
        private GameplayEvents _gameplayEvents;

        [SerializeField] private SpaceViewUIManager spaceViewUI;
        [SerializeField] private Sprite[] planetIcons;
        [SerializeField] private ExplorerControlUIManager explorerControlUI;
        [SerializeField] private EnergyControlUIManager energyControlUI;
        [SerializeField] private PlanetIndexUIManager planetIndexUI;
        [SerializeField] private MainHUDSettingsPanel settingUI;
        [SerializeField] private PlanetInfoUIManager planetInfo;
        [SerializeField] private HoveredGridInfo hoveredGridInfo;
        [SerializeField] private LeaderboardUIManager leaderboardUI;
        [SerializeField] private LiveTextNotifUIManager liveTextNotifUI;

        public void Init(EventManager eventManager)
        {
            _gameplayEvents = eventManager.GameplayEvents;
            gameObject.SetActive(false);
            explorerControlUI.Init(_gameplayEvents);
            energyControlUI.Init(_gameplayEvents);
            energyControlUI.gameObject.SetActive(false);
            planetIndexUI.Init(_gameplayEvents, planetIcons);
            settingUI.Init(eventManager.GeneralEvents);
            settingUI.gameObject.SetActive(false);
            planetInfo.Init(eventManager, planetIcons);
            planetInfo.gameObject.SetActive(false);
            hoveredGridInfo.Init(eventManager);
            spaceViewUI.Init(eventManager, GetComponent<RectTransform>());
            leaderboardUI.Init(eventManager);
            liveTextNotifUI.Init(eventManager);
        }

        private void OnEnable()
        {
            if (_gameplayEvents != null)
            {
                _gameplayEvents.PlanetSelected += OnPlanetSelected;
                _gameplayEvents.PlanetDeselected += OnPlanetDeselected;
            }
        }

        private void OnDisable()
        {
            if (_gameplayEvents != null)
            {
                _gameplayEvents.PlanetSelected -= OnPlanetSelected;
                _gameplayEvents.PlanetDeselected -= OnPlanetDeselected;
            }
        }

        private void OnPlanetSelected(Planet selectedPlanet, bool isPlanetOwned, SpaceEnvironment environment)
        {
            if(isPlanetOwned)
                energyControlUI.OpenEnergyControl(selectedPlanet);
            planetInfo.Show(selectedPlanet, isPlanetOwned, UIUtility.SpaceAreaString(environment));
        }

        private void OnPlanetDeselected()
        {
            planetInfo.gameObject.SetActive(false);
        }
    }
}
