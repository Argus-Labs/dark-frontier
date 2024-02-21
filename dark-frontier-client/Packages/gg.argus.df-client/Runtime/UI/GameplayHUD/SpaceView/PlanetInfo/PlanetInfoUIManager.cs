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
using ArgusLabs.DF.Core.Space;
using UnityEngine;

namespace ArgusLabs.DF.UI.GameplayHUD.SpaceView.PlanetInfo
{
    public class PlanetInfoUIManager : MonoBehaviour
    {
        [SerializeField] private MainHUDPlanetInfo playerPlanetInfo;
        [SerializeField] private MainHUDPlanetInfo enemyPlanetInfo;
        [SerializeField] private MainHUDPlanetInfo unclaimedPlanetInfo;

        private EventManager _eventManager;
        private Planet _planet;
        private MainHUDPlanetInfo _shownInfoPanel;

        public void Init(EventManager eventManager, Sprite[] planetIcons)
        {
            _eventManager = eventManager;

            playerPlanetInfo.Init(eventManager, planetIcons);
            enemyPlanetInfo.Init(eventManager, planetIcons);
            unclaimedPlanetInfo.Init(eventManager, planetIcons);
        }

        private void OnEnable()
        {
            if (_eventManager != null)
            {
                _eventManager.GameplayEvents.PlanetUpdated += OnPlanetEnergyUpdated;
                _eventManager.GameplayEvents.PlanetControlChanged += OnPlanetControlChanged;
            }
        }

        private void OnDisable()
        {
            if (_eventManager != null)
            {
                _eventManager.GameplayEvents.PlanetUpdated -= OnPlanetEnergyUpdated;
                _eventManager.GameplayEvents.PlanetControlChanged -= OnPlanetControlChanged;
            }
        }

        private void OnPlanetEnergyUpdated(Planet planet)
        {
            if (planet.Id == _planet.Id)
            {
                _shownInfoPanel.SetPlanetCurrentEnergy(planet.RoundedEnergyLevel);
            }
        }

        private void OnPlanetControlChanged(Planet obj, Player newOwner, Player prevOwner) //changes in ownership will need the shown info to change
        {
            if(obj.Id == _planet.Id)
            {
                Show(obj, newOwner.IsLocal, "");
            }
        }

        public void Show(Planet obj, bool isOwned, string spaceArea)
        {
            gameObject.SetActive(true);
            _planet = obj;

            if(_shownInfoPanel != null) _shownInfoPanel.gameObject.SetActive(false);
            if (isOwned)
            {
                _shownInfoPanel = playerPlanetInfo;
            }
            else
            {
                _shownInfoPanel = obj.IsClaimed ? enemyPlanetInfo : unclaimedPlanetInfo;
            }
            _shownInfoPanel.Show(obj, isOwned, spaceArea);
        }

        private void ClosePanel()
        {
            _eventManager.GameplayEvents.PlanetInfoClosed?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
