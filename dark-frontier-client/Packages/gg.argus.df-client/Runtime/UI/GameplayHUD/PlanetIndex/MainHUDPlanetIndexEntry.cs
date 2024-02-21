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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD.PlanetIndex
{
    public class MainHUDPlanetIndexEntry : MonoBehaviour
    {
        [SerializeField] private Image _image_Planet;

        [SerializeField] private TextMeshProUGUI _energyAmountText;
        [SerializeField] private Image _image_EnergyFill;

        [SerializeField] private GameObject _transferStatusPanel;
        [SerializeField] private Image _image_EnergySendStatus;
        [SerializeField] private Image _image_FriendlyTransferStatus;
        [SerializeField] private Image _image_HostileTransferStatus;

        [SerializeField] private TextMeshProUGUI _text_PlanetLevel;
        [SerializeField] private TextMeshProUGUI _text_PlanetPosition;

        public Planet Planet { get; private set; }

        private Sprite[] _sprite_Planets;

        private GameplayEvents _eventManager;

        public void Init(GameplayEvents eventManager, Sprite[] planetIcons)
        {
            _eventManager = eventManager;
            _sprite_Planets = planetIcons;
        }

        private void OnEnable()
        {
            if (_eventManager != null)
            {
                _eventManager.PlanetUpdated += OnPlanetEnergyUpdated;
                _eventManager.PlanetExportUpdated += OnPlanetExportCountUpdated;
            }
        }

        private void OnDisable()
        {
            if (_eventManager != null)
            {
                _eventManager.PlanetUpdated -= OnPlanetEnergyUpdated;
                _eventManager.PlanetExportUpdated -= OnPlanetExportCountUpdated;
            }
        }

        private void OnPlanetEnergyUpdated(Planet planet)
        {
            if (Planet == null) return;
            if (planet.Id == Planet.Id)
            {
                SetEnergyBar((float)planet.Progress);
                SetEnergyAmount(planet.RoundedEnergyLevel);
            }
        }

        private void OnPlanetExportCountUpdated(Planet planet)
        {
            if (planet.Id == Planet.Id)
            {
                SetEnergySendingStatus(planet.GetBiggestExport() > -1);
                RecheckTransferStatusPanel();
            }
        }

        public void Show(Planet planet)
        {
            Planet = planet;
            _text_PlanetLevel.text = planet.Level.ToString();
            _text_PlanetPosition.text = planet.Position.ToString();
            SetPlanetImage(planet.Level);

            SetEnergySendingStatus(planet.GetBiggestExport() > -1);
            SetFriendlyReceivingStatus(planet.GetBiggestFriendlyInbound() > -1);
            SetHostileReceivingStatus(planet.GetBiggestHostileInbound() > -1);
            RecheckTransferStatusPanel();

            float normalizedProgress = (float)(planet.EnergyLevel / planet.EnergyCapacity);
            SetEnergyAmount((int)planet.EnergyLevel);
            SetEnergyBar(normalizedProgress);
        }

        private void SetPlanetImage(int in_Level)
        {
            int planetLevel = Mathf.Clamp(in_Level, 0, _sprite_Planets.Length);
            _image_Planet.sprite = _sprite_Planets[planetLevel];
        }

        private void SetEnergyAmount(int energy)
        {
            _energyAmountText.text = energy.ToString(); //a ToString() in update, potentially bad for memory
        }

        private void SetEnergyBar(float in_NormalizedRefillProgress)
        {
            _image_EnergyFill.fillAmount = in_NormalizedRefillProgress;
        }

        private void SetEnergySendingStatus(bool in_Status)
        {
            _image_EnergySendStatus.gameObject.SetActive(in_Status);
        }

        private void SetFriendlyReceivingStatus(bool in_Status)
        {
            _image_FriendlyTransferStatus.gameObject.SetActive(in_Status);
        }

        private void SetHostileReceivingStatus(bool in_Status)
        {
            _image_HostileTransferStatus.gameObject.SetActive(in_Status);
        }

        private void RecheckTransferStatusPanel()
        {
            if(_image_HostileTransferStatus.gameObject.activeSelf || _image_FriendlyTransferStatus.gameObject.activeSelf || _image_EnergySendStatus.gameObject.activeSelf)
            {
                _transferStatusPanel.gameObject.SetActive(true);
            }
            else
            {
                _transferStatusPanel.gameObject.SetActive(false);
            }
        }
    }
}
