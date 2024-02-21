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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD.SpaceView.PlanetEnergy
{

    //no longer used
    public class SpaceViewPlanetEnergyBar : MonoBehaviour
    {
        [SerializeField] private Image _energyBar;
        [SerializeField] private TextMeshProUGUI _energyAmount;
        [SerializeField] private Color _localPlayerBarColor;
        [SerializeField] private Color _enemyBarColor;
        [SerializeField] private Color _unclaimedBarColor;

        private EventManager _eventManager;
        private Vector2 _planetGridPos;
        private Transform _canvasScale;
        private Camera _camera;
        private float _planetScale;

        public string PlanetID { get; private set; }

        public void Init(EventManager eventManager, Transform canvasScale)
        {
            _eventManager = eventManager;
            _canvasScale = canvasScale;
            _camera = Camera.main;

            GetComponent<RectTransform>().anchorMin = Vector3.zero;
            GetComponent<RectTransform>().anchorMax = Vector3.zero;
        }

        private void OnEnable()
        {
            if(_eventManager != null )
            {
                _eventManager.GameplayEvents.PlanetUpdated += OnPlanetEnergyUpdated;
                _eventManager.GameplayEvents.PlanetHoverStarted += OnPlanetHoverStarted;
                _eventManager.GameplayEvents.PlanetHoverEnded += OnPlanetHoverEnded;
                _eventManager.GameplayEvents.PlanetControlChanged += OnPlanetControlChanged;
                _eventManager.GameplayEvents.CameraZoomChanged += OnCameraZoomChanged;
            }
            
            throw new Exception("If this class is being used, please remove the comment regarding its disuse.");
        }

        private void OnDisable()
        {
            if (_eventManager != null)
            {
                _eventManager.GameplayEvents.PlanetUpdated -= OnPlanetEnergyUpdated;
                _eventManager.GameplayEvents.PlanetHoverStarted -= OnPlanetHoverStarted;
                _eventManager.GameplayEvents.PlanetHoverEnded -= OnPlanetHoverEnded;
                _eventManager.GameplayEvents.PlanetControlChanged -= OnPlanetControlChanged;
                _eventManager.GameplayEvents.CameraZoomChanged -= OnCameraZoomChanged;
            }
        }

        private void OnPlanetEnergyUpdated(Planet planet)
        {
            if (planet.Id == PlanetID)
            {
                SetEnergyAmount(planet.RoundedEnergyLevel);
                SetEnergyBarFill((float)planet.Progress);
            }
        }

        private void OnPlanetHoverStarted(Planet planet, bool isOwned)
        {
            if (planet.Id == PlanetID)
            {
                ShowEnergyAmount(true);
            }
        }

        private void OnPlanetHoverEnded()
        {
            ShowEnergyAmount(false);
        }

        private void OnPlanetControlChanged(Planet planet, Player newOwner, Player prevOwner)
        {
            if(planet.Id == PlanetID)
            {
                SetEnergyBarColor(newOwner.IsLocal, planet.IsClaimed);
            }
        }

        private void OnCameraZoomChanged(float obj)
        {
            SetScale(obj);
        }

        public void Show(Planet obj, bool isOwned)
        {
            gameObject.SetActive(true);
            PlanetID = obj.Id;
            _planetGridPos = obj.Position;
            SetEnergyBarFill((float)obj.EnergyLevel / (float)obj.EnergyCapacity);
            SetEnergyBarColor(isOwned, obj.IsClaimed);
            _planetScale = obj.Scale;
            //this is assuming _camera is the same as spaceCamera that is modified when zooming
            SetScale(_camera.orthographicSize);
        }

        private void LateUpdate()
        {
            Vector3 screenPos = _camera.WorldToScreenPoint(new Vector3(_planetGridPos.x + 0.5f, _planetGridPos.y + 0.5f, 0)) / _canvasScale.localScale.x;
            GetComponent<RectTransform>().anchoredPosition = screenPos;
        }

        private void SetEnergyBarColor(bool isOwned, bool isClaimed)
        {
            if (isOwned) _energyBar.color = _localPlayerBarColor;
            else
            {
                if (isClaimed) _energyBar.color = _enemyBarColor;
                else _energyBar.color = _unclaimedBarColor;
            }
        }

        private void SetEnergyBarFill(float in_FillAmount)
        {
            _energyBar.fillAmount = Mathf.Clamp01(in_FillAmount);
        }

        private void ShowEnergyAmount(bool in_Status)
        {
            _energyAmount.gameObject.SetActive(in_Status);
        }

        private void SetEnergyAmount(int in_Energy)
        {
            _energyAmount.text = in_Energy.ToString();
        }

        private void SetScale(float orthographicSize)
        {
            GetComponent<RectTransform>().localScale = new Vector2(0.3f + (_planetScale * 10f / orthographicSize), 0.3f + (_planetScale * 10f / orthographicSize));
        }
    }
}
