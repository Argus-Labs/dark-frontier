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

using System.Collections.Generic;
using ArgusLabs.DF.Core;
using ArgusLabs.DF.Core.Space;
using ArgusLabs.DF.UI.GameplayHUD.SpaceView.EnergyTransferPreview;
using ArgusLabs.DF.UI.GameplayHUD.SpaceView.PlanetEnergy;
using UnityEngine;

namespace ArgusLabs.DF.UI.GameplayHUD
{
    public class SpaceViewUIManager : MonoBehaviour
    {
        [SerializeField] private SpaceViewEnergyTransferOngoing _energyTransferInfoPrefab;
        [SerializeField] private SpaceViewPlanetEnergyText _energyTextPrefab;
        [SerializeField] private Transform _planetEnergyTextParent;
        [SerializeField] private Transform _energyTransferInfoParent;
        [SerializeField] private SpaceViewEnergyTransferPreview _energyTransferPreview;

        private List<SpaceViewPlanetEnergyText> _energyTextPool;
        private List<SpaceViewEnergyTransferOngoing> _energyTransferInfoPool;
        private EventManager _eventManager;
        private Transform _scalerTransform; //for canvas scaler
        
        public void Init(EventManager eventManager, Transform scalerTransform)
        {
            _eventManager = eventManager;
            _scalerTransform = scalerTransform;
            _energyTextPool = new List<SpaceViewPlanetEnergyText>();
            _energyTransferInfoPool = new List<SpaceViewEnergyTransferOngoing>();
            _energyTransferPreview.Init(eventManager, scalerTransform);
            _energyTransferPreview.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_eventManager != null)
            {
                _eventManager.GameplayEvents.EnergyTransferStateEntered += OnEnergyTransferStateEntered;
                _eventManager.GameplayEvents.EnergyTransferStateExited += OnEnergyTransferStateExited;
                _eventManager.GameplayEvents.EnergyTransferStarted += OnEnergyTransferStarted;
                _eventManager.GameplayEvents.PlanetShown += OnPlanetShown;
            }
        }

        private void OnDisable()
        {
            if (_eventManager != null)
            {
                _eventManager.GameplayEvents.EnergyTransferStateEntered -= OnEnergyTransferStateEntered;
                _eventManager.GameplayEvents.EnergyTransferStateExited -= OnEnergyTransferStateExited;
                _eventManager.GameplayEvents.EnergyTransferStarted -= OnEnergyTransferStarted;
                _eventManager.GameplayEvents.PlanetShown -= OnPlanetShown;
            }
        }

        private void OnEnergyTransferStateExited()
        {
            _energyTransferPreview.gameObject.SetActive(false);
        }

        private void OnEnergyTransferStateEntered()
        {
            _energyTransferPreview.gameObject.SetActive(true);
        }

        private void OnEnergyTransferStarted(ClientEnergyTransfer transfer, Vector2 pos, float rotation)
        {
            for (int i = 0; i < _energyTransferInfoPool.Count; i++)
            {
                if (!_energyTransferInfoPool[i].gameObject.activeInHierarchy)
                {
                    _energyTransferInfoPool[i].Show(transfer, pos, rotation);
                    return;
                }
            }

            // I've updated it a little and it becomes like this, the nullref doesn't happen again
            SpaceViewEnergyTransferOngoing newEnergyTransferInfo = Instantiate(_energyTransferInfoPrefab, _energyTransferInfoParent);
            newEnergyTransferInfo.Init(_eventManager, _scalerTransform);
            newEnergyTransferInfo.Show(transfer, pos, rotation);
            _energyTransferInfoPool.Add(newEnergyTransferInfo);
        }

        private void OnPlanetShown(Planet planet)
        {
            for (int i = 0; i < _energyTextPool.Count; i++)
            {
                if (!_energyTextPool[i].gameObject.activeInHierarchy)
                {
                    _energyTextPool[i].Show(planet); //, _player.Owns(planet));
                    return;
                }
            }

            SpaceViewPlanetEnergyText newEnergyText = Instantiate(_energyTextPrefab, _planetEnergyTextParent);
            newEnergyText.Init(_eventManager, _scalerTransform);
            newEnergyText.gameObject.SetActive(false); //need to be deactivated first because it didn't listen to events the first time it's instantiated
            newEnergyText.Show(planet); //, _player.Owns(planet));
            _energyTextPool.Add(newEnergyText);
        }
    }
}
