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
using ArgusLabs.DF.Core;
using ArgusLabs.DF.Core.Space;
using ArgusLabs.DF.UI.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD
{
    public class MainHUDPlanetInfo : MonoBehaviour
    {
        [SerializeField] private Image _planetImage;
        [SerializeField] private TextMeshProUGUI _text_PlanetName;
        [SerializeField] private TextMeshProUGUI _text_PlanetPosition;
        [SerializeField] private TextMeshProUGUI _text_SpaceAreaType;
        [SerializeField] private TextMeshProUGUI _text_PlanetOwner;
        [SerializeField] private TextMeshProUGUI _text_PlanetLevel;
        [SerializeField] private TextMeshProUGUI _text_PlanetEnergy;
        [SerializeField] private TextMeshProUGUI _text_PlanetEnergyMax;
        [SerializeField] private TextMeshProUGUI _text_PlanetEnergyGrowth;
        [SerializeField] private TextMeshProUGUI _text_PlanetDefense;
        [SerializeField] private TextMeshProUGUI _text_PlanetRange;
        [SerializeField] private TextMeshProUGUI _text_PlanetSpeed;

        private EventManager _eventManager;
        private Planet _planet;
        private Sprite[] _planetIcons;

        public void Init(EventManager eventManager, Sprite[] planetIcons)
        {
            _eventManager = eventManager;
            _planetIcons = planetIcons;
        }

        private void OnEnable()
        {
            if (_eventManager != null)
            {
                _eventManager.GameplayEvents.PlanetUpdated += OnPlanetEnergyUpdated;
            }
        }

        private void OnDisable()
        {
            if (_eventManager != null)
            {
                _eventManager.GameplayEvents.PlanetUpdated -= OnPlanetEnergyUpdated;
            }
        }

        private void OnPlanetEnergyUpdated(Planet planet)
        {
            if (planet.Id == _planet.Id)
            {
                SetPlanetCurrentEnergy(planet.RoundedEnergyLevel);
            }
        }

        public void Show(Planet obj, bool isOwned, string spaceArea = "")
        {
            gameObject.SetActive(true);
            _planet = obj;

            _text_PlanetPosition.text = obj.Position.ToString();
            _text_PlanetName.text = obj.Name;

            SetOwnerDisplay(obj, isOwned);
            SetPlanetLevel(obj.Level);
            SetPlanetCurrentEnergy(Mathf.RoundToInt((float)obj.EnergyLevel));
            SetPlanetStats(Mathf.RoundToInt((float)obj.EnergyCapacity), Mathf.RoundToInt((float)obj.EnergyRefillPeriod), obj.Speed, obj.FullRange, Mathf.RoundToInt((float)obj.Defense));
            SetPlanetImage(obj.Level);
            if(!string.IsNullOrEmpty(spaceArea))
                _text_SpaceAreaType.text = spaceArea;
        }

        private void SetOwnerDisplay(Planet planet, bool isOwned)
        {
            _text_PlanetOwner.color = ColorPalette.LocalPlayer;
            if (!planet.IsClaimed)
            {
                _text_PlanetOwner.text = "unclaimed";
            }
            else
            {
                _text_PlanetOwner.text = UIUtility.TrimLongString(planet.Owner.PersonaTag, 12);
            }
        }

        private void SetPlanetImage(int in_Level)
        {
            int planetLevel = Mathf.Clamp(in_Level, 0, _planetIcons.Length);
            _planetImage.sprite = _planetIcons[planetLevel];
        }

        public void SetPlanetLevel(int in_PlanetLevel)
        {
            _text_PlanetLevel.text = $"LEVEL {in_PlanetLevel.ToString()}";
        }

        public void SetPlanetStats(int in_EnergyMax, int in_FillTime, double in_Speed, int in_Range, int in_Defense)
        {
            _text_PlanetEnergyMax.text = in_EnergyMax.ToString();
            _text_PlanetEnergyGrowth.text = UIUtility.TimeSpanToString(TimeSpan.FromSeconds(in_FillTime));
            _text_PlanetSpeed.text = in_Speed.ToString("F2");
            _text_PlanetRange.text = in_Range.ToString();
            _text_PlanetDefense.text = in_Defense.ToString();
        }

        public void SetPlanetCurrentEnergy(int in_CurrentEnergy)
        {
            _text_PlanetEnergy.text = in_CurrentEnergy.ToString();
        }
    }
}
