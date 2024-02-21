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
using Smonch.CyclopsFramework.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD
{
    public class EnergyControlUIManager : MonoBehaviour
    {
        [SerializeField] private Animator _animator_EnergyControl;

        [SerializeField] private TextMeshProUGUI _text_CurrentEnergy;
        [SerializeField] private TextMeshProUGUI _text_MaxEnergy;
        [SerializeField] private TextMeshProUGUI _text_SetEnergy;
        [SerializeField] private TextMeshProUGUI _text_CurrentEnergyLabel;
        [SerializeField] private TextMeshProUGUI _text_SetEnergyLabel;

        [SerializeField] private Slider _slider_SetEnergy;
        //[SerializeField] private Image _image_CurrentEnergyBar;

        [SerializeField] private Image[] _image_ShipsAvailable;     // Currently set to a max of 6

        [SerializeField] private Color shipIconColor;

        private GameplayEvents _eventManager;

        private string _selectedPlanetId;
        private int _currentEnergy;
        private int _maxEnergy;
        private int _setEnergy;
        private int _setEnergyPercentage = 50; //current percent pointed and set in the slider

        //private int _energyPercentTarget = 50; //energy percent that is manually selected by player

        public void Init(GameplayEvents eventManager)
        {
            _eventManager = eventManager;
        }

        private void OnEnable()
        {
            _slider_SetEnergy.value = 0; // TEST - To remove

            _slider_SetEnergy.onValueChanged.AddListener(UpdateEnergySetPercentage);
            if (_eventManager != null)
            {
                _eventManager.PlanetDeselected += OnPlanetDeselected;
                _eventManager.PlanetUpdated += OnPlanetEnergyUpdated;
                _eventManager.PlanetExportUpdated += OnSelectedPlanetExportCountUpdated;
            }
        }

        private void OnDisable()
        {
            _slider_SetEnergy.onValueChanged.RemoveAllListeners();
            if (_eventManager != null)
            {
                _eventManager.PlanetDeselected -= OnPlanetDeselected;
                _eventManager.PlanetUpdated -= OnPlanetEnergyUpdated;
                _eventManager.PlanetExportUpdated -= OnSelectedPlanetExportCountUpdated;
            }
        }

        private void Update()
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                DecreaseEnergyPercentageToSend();
            }
            else if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                IncreaseEnergyPercentageToSend();
            }
        }

        /// <summary>
        /// open energy control and set the energy display corresponding to the selected planet, this is not made as an event handler in this class 
        /// because energy control is disabled at default, so the event handler is in GameplayUIManager then it calls this function
        /// </summary>
        /// <param name="args"></param>
        public void OpenEnergyControl(Planet args)
        {
            gameObject.SetActive(true);
            //_animator_EnergyControl.SetBool("isOpened", true);
            _selectedPlanetId = args.Id;
            //SetEnergyMax(Mathf.RoundToInt((float)args.EnergyCapacity));
            _slider_SetEnergy.value = Mathf.RoundToInt(_setEnergyPercentage);
            SetEnergyCurrent(Mathf.RoundToInt((float)args.EnergyLevel));
            SetShipAvailableCount(args.ExportShipRemaining);
        }

        private void OnPlanetDeselected()
        {
            _selectedPlanetId = "";
            gameObject.SetActive(false);
            //_animator_EnergyControl.SetBool("isOpened", false);
        }

        private void OnPlanetEnergyUpdated(Planet planet)
        {
            if (planet.Id == _selectedPlanetId)
            {
                SetEnergyCurrent(planet.RoundedEnergyLevel);
            }
        }

        private void OnSelectedPlanetExportCountUpdated(Planet planet)
        {
            if (planet.Id == _selectedPlanetId)
            {
                SetShipAvailableCount(planet.ExportShipRemaining);
            }
        }

        private void UpdateEnergySetPercentage(float in_Value)
        {
            int valueInPercentage = Mathf.RoundToInt(in_Value);
            _setEnergyPercentage = valueInPercentage;
            UpdateEnergySet();
            _eventManager.EnergyTransferPercentUpdated?.Invoke(_setEnergyPercentage);
        }

        public void CloseEnergyControl()
        {
            gameObject.SetActive(false);
        }

        private void SetShipAvailableCount(int availableShip)
        {
            for (int i = 0; i < _image_ShipsAvailable.Length; i++)
            {
                if (i < availableShip)
                {
                    _image_ShipsAvailable[i].color = shipIconColor.WithAlpha(0.25f);
                }
                else
                {
                    _image_ShipsAvailable[i].color = shipIconColor.WithAlpha(1);
                }
            }
        }

        public void SetEnergyMax(int in_Amount)
        {
            _maxEnergy = in_Amount;

            _text_MaxEnergy.text = _maxEnergy.ToString();
        }

        public void SetEnergyCurrent(int in_Amount)
        {
            _currentEnergy = in_Amount;

            _text_CurrentEnergy.text = _currentEnergy.ToString();

            UpdateEnergySet();
        }

        private void UpdateEnergySet()
        {
            _setEnergy = Mathf.RoundToInt(_currentEnergy * _setEnergyPercentage / 100f);
            _text_SetEnergy.text = _setEnergy.ToString();
        }

        private void IncreaseEnergyPercentageToSend()
        {
            if (_slider_SetEnergy.value < 100)
            {
                if (_slider_SetEnergy.value < 90)
                    _slider_SetEnergy.value += 10;
                else _slider_SetEnergy.value = 100;
            }
        }

        private void DecreaseEnergyPercentageToSend()
        {
            if (_slider_SetEnergy.value > 0)
            {
                if (_slider_SetEnergy.value > 10)
                    _slider_SetEnergy.value -= 10;
                else _slider_SetEnergy.value = 0;
            }
        }
    }
}
