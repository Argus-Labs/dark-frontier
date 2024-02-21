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
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArgusLabs.DF.UI.GameplayHUD.SpaceView.EnergyTransferPreview
{
    public class SpaceViewEnergyTransferPreview : SpaceViewEnergyTransferUI
    {
        private float _targetRotation;
        private float _rotateLerpProgress;
        private bool _isJustEnabled; //a flag to note that we want to set the rotation immediately in the first frame this is enabled

        protected override void ListenEvents()
        {
            _eventManager.GameplayEvents.EnergyTransferPreview += OnEnergyTransferPreview;
            base.ListenEvents();
        }

        protected override void UnlistenEvents()
        {
            _eventManager.GameplayEvents.EnergyTransferPreview -= OnEnergyTransferPreview;
            base.UnlistenEvents();
        }

        private void OnEnable()
        {
            _isJustEnabled = true;
        }

        private void OnEnergyTransferPreview(float infoArmRotation, TimeSpan duration, int energy)
        {
            if (_energyTransferId != 0)
                return;

            if (_isJustEnabled)
            {
                _rotatingAnchor.localRotation = Quaternion.Euler(0, 0, infoArmRotation * Mathf.Rad2Deg);
                _rotatingPanel.localRotation = Quaternion.Euler(0, 0, -infoArmRotation * Mathf.Rad2Deg);
                _isJustEnabled = false;
            }
            else
            {
                if (!Mathf.Approximately(_targetRotation, infoArmRotation))
                {
                    _targetRotation = infoArmRotation * Mathf.Rad2Deg;
                    _rotateLerpProgress = 0;
                }
            }
            SetTime(duration, "<1s");
            SetEnergyAmount(energy);

            if (energy <= 0)
                SetOutOfRange();

            Vector3 screenPos = Mouse.current.position.value / _canvasScale.localScale.x;
            GetComponent<RectTransform>().anchoredPosition = screenPos;
        }

        private void SetOutOfRange()
        {
            _text_Time.text = "OUT OF RANGE";
            _text_Time.color = Color.red;
            _separator.SetActive(false);
            _energyAmount.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_rotateLerpProgress < 1)
            {
                _rotateLerpProgress += Time.deltaTime * 10;
                float currentRotation = Mathf.LerpAngle(_rotatingAnchor.localRotation.eulerAngles.z, _targetRotation, _rotateLerpProgress);

                _rotatingAnchor.localRotation = Quaternion.Euler(0, 0, currentRotation);
                _rotatingPanel.localRotation = Quaternion.Euler(0, 0, -currentRotation);
            }
        }
    }
}
