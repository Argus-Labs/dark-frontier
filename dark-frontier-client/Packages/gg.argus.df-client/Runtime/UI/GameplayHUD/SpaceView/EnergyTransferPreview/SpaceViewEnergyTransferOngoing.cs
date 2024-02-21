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
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD.SpaceView.EnergyTransferPreview
{
    public class SpaceViewEnergyTransferOngoing : SpaceViewEnergyTransferUI
    {
        [SerializeField] private Image _infoArm;
        [SerializeField] private Image _divider;
        [SerializeField] private Image _energyIcon;

        public override void Show(ClientEnergyTransfer transfer, Vector2 position, float rotation)
        {
            base.Show(transfer, position, rotation);
            _rotatingAnchor.localRotation = Quaternion.Euler(0,0, rotation * Mathf.Rad2Deg);
            _rotatingPanel.localRotation = Quaternion.Euler(0, 0,-rotation * Mathf.Rad2Deg);
            _infoArm.color = transfer.ShipOwner.IsLocal ? ColorPalette.LocalPlayer : ColorPalette.Enemy;
            Color textsColor = transfer.ShipOwner.IsLocal ? ColorPalette.LocalPlayer : transfer.Source == null ? ColorPalette.Enemy : transfer.ShipOwner.Color;
            SetTextColor(textsColor);
        }

        protected override void ListenEvents()
        {
            if (!_isListeningEvents && _eventManager != null)
            {
                _eventManager.GameplayEvents.EnergyTransferUpdated += OnEnergyTransferUpdated;
                _eventManager.GameplayEvents.EnergyTransferArrived += OnEnergyTransferArrived;
            }
            base.ListenEvents();
        }

        protected override void UnlistenEvents()
        {
            if (_isListeningEvents && _eventManager != null)
            {
                _eventManager.GameplayEvents.EnergyTransferUpdated -= OnEnergyTransferUpdated;
                _eventManager.GameplayEvents.EnergyTransferArrived -= OnEnergyTransferArrived;
            }
            base.UnlistenEvents();
        }

        private void OnEnergyTransferUpdated(ulong id, Vector2 newPosition, TimeSpan durationRemaining)
        {
            if (_energyTransferId != id)
                return;

            SetTime(durationRemaining, "Landing");
            Vector3 screenPos = newPosition / _canvasScale.localScale.x;
            GetComponent<RectTransform>().anchoredPosition = screenPos;
        }

        private void OnEnergyTransferArrived(ClientEnergyTransfer transfer)
        {
            if (_energyTransferId != transfer.Id)
                return;

            _energyTransferId = 0;
            gameObject.SetActive(false);
        }
        private void SetTextColor(Color color)
        {
            _text_Time.color = color;
            _text_EnergyAmount.color = color;
            _divider.color = color;
            _energyIcon.color = color;
        }
    }
}
