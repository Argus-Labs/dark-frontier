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
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD.PlanetIndex
{
    public class PlanetIndexEntryRoot : MonoBehaviour
    {
        [SerializeField] private MainHUDPlanetIndexEntry _attackedVariant;
        [SerializeField] private MainHUDPlanetIndexEntry _unselectedVariant;
        [SerializeField] private MainHUDPlanetIndexEntry _selectedVariant;
        [SerializeField] private Button _button_PlanetEntry;

        public Planet Planet { get; private set; }

        private GameplayEvents _eventManager;
        private bool _isSelected = false;
        private bool _isAttacked = false;

        public void Init(GameplayEvents eventManager, Sprite[] planetIcons)
        {
            _eventManager = eventManager;
            _attackedVariant.Init(eventManager, planetIcons);
            _unselectedVariant.Init(eventManager, planetIcons);
            _selectedVariant.Init(eventManager, planetIcons);
        }

        private void OnEnable()
        {
            _button_PlanetEntry.onClick.AddListener(SelectPlanetIndexEntry);

            if (_eventManager != null)
            {
                _eventManager.PlanetIncomingTransferUpdated += OnPlanetIncomingTransferUpdated;
                _eventManager.PlanetSelected += OnPlanetSelected;
                _eventManager.PlanetDeselected += OnPlanetDeselected;
            }
        }

        private void OnDisable()
        {
            _button_PlanetEntry.onClick.RemoveAllListeners();

            if (_eventManager != null)
            {
                _eventManager.PlanetIncomingTransferUpdated -= OnPlanetIncomingTransferUpdated;
                _eventManager.PlanetSelected -= OnPlanetSelected;
                _eventManager.PlanetDeselected -= OnPlanetDeselected;
            }
        }

        private void SelectPlanetIndexEntry()
        {
            _eventManager.PlanetIndexSelectPlanet?.Invoke(Planet.Position);
        }

        private void OnPlanetSelected(Planet planet, bool arg2, SpaceEnvironment arg3)
        {
            if (planet.Id == Planet.Id)
            {
                _isSelected = true;
                ShowState(Planet, EntryState.Selected);
            }
            else
            {
                if (_isSelected)
                {
                    _isSelected = false;
                    Show(Planet, _isSelected);
                }
            }
        }

        private void OnPlanetDeselected()
        {
            if (_isSelected)
            {
                _isSelected = false;
                Show(Planet, _isSelected);
            }
        }

        public void Show(Planet planet, bool isSelected)
        {
            Planet = planet;
            _isSelected = isSelected;
            _isAttacked = planet.GetBiggestHostileInbound() > -1;
            if (_isSelected)
            {
                ShowState(Planet, EntryState.Selected);
            }
            else if (_isAttacked)
            {
                ShowState(Planet, EntryState.Attacked);
            }
            else
            {
                ShowState(Planet, EntryState.Unselected);
            }
        }

        private void OnPlanetIncomingTransferUpdated(Planet planet)
        {
            if (planet.Id == Planet.Id)
            {
                if (!_isSelected)
                {
                    if (planet.GetBiggestHostileInbound() > -1)
                    {
                        ShowState(Planet, EntryState.Attacked);
                    }
                    else
                    {
                        ShowState(Planet, EntryState.Unselected);
                    }
                }
                _isAttacked = planet.GetBiggestHostileInbound() > -1;
            }
        }

        private void ShowState(Planet planet, EntryState state) //0:unselected, 1: attacked, 2: selected
        {
            switch (state)
            {
                case EntryState.Unselected:
                    {
                        _unselectedVariant.gameObject.SetActive(true);
                        _unselectedVariant.Show(planet);
                        _attackedVariant.gameObject.SetActive(false);
                        _selectedVariant.gameObject.SetActive(false);
                        break;
                    }
                case EntryState.Attacked:
                    {
                        _attackedVariant.gameObject.SetActive(true);
                        _attackedVariant.Show(planet);
                        _unselectedVariant.gameObject.SetActive(false);
                        _selectedVariant.gameObject.SetActive(false);
                        break;
                    }
                case EntryState.Selected:
                    {
                        _selectedVariant.gameObject.SetActive(true);
                        _selectedVariant.Show(planet);
                        _unselectedVariant.gameObject.SetActive(false);
                        _attackedVariant.gameObject.SetActive(false);
                        break;
                    }
                default:
                    {
                        _unselectedVariant.gameObject.SetActive(true);
                        _unselectedVariant.Show(planet);
                        _attackedVariant.gameObject.SetActive(false);
                        _selectedVariant.gameObject.SetActive(false);
                        break;
                    }
            }
        }

        private enum EntryState
        {
            Unselected,
            Attacked,
            Selected
        }

    }
}
