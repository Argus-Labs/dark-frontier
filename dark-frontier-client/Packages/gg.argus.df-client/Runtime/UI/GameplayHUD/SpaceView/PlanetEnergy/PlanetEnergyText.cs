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
using UnityEngine.Assertions;

namespace ArgusLabs.DF.UI.GameplayHUD.SpaceView.PlanetEnergy
{
    public class SpaceViewPlanetEnergyText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _energyAmount;
        [SerializeField] private TextMeshProUGUI _netEnergyTransferredAmount;

        private EventManager _eventManager;
        //private Vector2 _planetGridPos;
        private Transform _canvasScale;
        private Camera _camera;
        //private float _planetScale;
        private float _zoomValue;
        //private string _planetId;
        private Planet _planet;
        private RectTransform _rectTransform;

        public void Init(EventManager eventManager, Transform canvasScale)
        {
            _eventManager = eventManager;
            _canvasScale = canvasScale;
            _camera = Camera.main;
            _netEnergyTransferredAmount.gameObject.SetActive(false);
            _rectTransform = GetComponent<RectTransform>();
            
            Assert.IsNotNull(_camera);
            
            _zoomValue = _camera.orthographicSize;
            _rectTransform.anchorMin = Vector3.zero;
            _rectTransform.anchorMax = Vector3.zero;
        }

        private void OnEnable()
        {
            if (_eventManager != null)
            {
                _eventManager.GameplayEvents.EnergyTransferTargetHoverStart += OnEnergyTransferTargetHoverStarted;
                _eventManager.GameplayEvents.EnergyTransferTargetHoverStop += OnEnergyTransferTargetHoverStopped;
                _eventManager.GameplayEvents.PlanetUpdated += OnPlanetEnergyUpdated;
                _eventManager.GameplayEvents.PlanetControlChanged += OnPlanetControlChanged;
                _eventManager.GameplayEvents.PlanetHidden += OnPlanetExitScreen;
                _eventManager.GameplayEvents.CameraZoomChanged += OnCameraZoomChanged;
            }
        }

        private void OnDisable()
        {
            if (_eventManager != null)
            {
                _eventManager.GameplayEvents.EnergyTransferTargetHoverStart -= OnEnergyTransferTargetHoverStarted;
                _eventManager.GameplayEvents.EnergyTransferTargetHoverStop -= OnEnergyTransferTargetHoverStopped;
                _eventManager.GameplayEvents.PlanetUpdated -= OnPlanetEnergyUpdated;
                _eventManager.GameplayEvents.PlanetControlChanged -= OnPlanetControlChanged;
                _eventManager.GameplayEvents.PlanetHidden -= OnPlanetExitScreen;
                _eventManager.GameplayEvents.CameraZoomChanged -= OnCameraZoomChanged;
            }
        }

        private void OnEnergyTransferTargetHoverStarted(string hoveredPlanetId, bool isOwnedPlanet, int netEnergyTransferred)
        {
            if (hoveredPlanetId == _planet.Id)
            {
                _netEnergyTransferredAmount.gameObject.SetActive(true);
                _netEnergyTransferredAmount.color = isOwnedPlanet ? Color.green : ColorPalette.Enemy;
                if (netEnergyTransferred > 0)
                {
                    _netEnergyTransferredAmount.text = $"{(isOwnedPlanet ? "(+" : "(-") + netEnergyTransferred})";
                }
                else
                {
                    _netEnergyTransferredAmount.text = string.Empty;
                }
            }
        }

        private void OnEnergyTransferTargetHoverStopped()
        {
            if (_netEnergyTransferredAmount.gameObject.activeInHierarchy)
            {
                _netEnergyTransferredAmount.gameObject.SetActive(false);
            }
        }

        private void OnPlanetEnergyUpdated(Planet planet)
        {
            if (planet == _planet)
            {
                _energyAmount.text = planet.RoundedEnergyLevel.ToString();
            }
        }

        private void OnPlanetControlChanged(Planet planet, Player newOwner, Player prevOwner)
        {
            if (planet == _planet)
            {
                _energyAmount.color = _planet.Owner.Color;
            }
        }

        private void OnPlanetExitScreen(Planet planet)
        {
            if (planet == _planet)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnCameraZoomChanged(float zoomValue)
        {
            _zoomValue = zoomValue;
        }

        public void Show(Planet planet)
        {
            _planet = planet;
            gameObject.SetActive(true);
            _energyAmount.color = _planet.Owner.Color;
            _zoomValue = _camera.orthographicSize;
            AdjustPosition();
        }

        private void LateUpdate()
        {
            AdjustPosition();
        }

        private void AdjustPosition()
        {
            Vector3 screenPos = _camera.WorldToScreenPoint(new Vector3(_planet.Position.x, _planet.Position.y - 0.1f - _planet.Scale * 0.7f - (_zoomValue * 0.65f / 20f), 0)) / _canvasScale.localScale.x;
            _rectTransform.anchoredPosition = screenPos;
        }
    }
}
