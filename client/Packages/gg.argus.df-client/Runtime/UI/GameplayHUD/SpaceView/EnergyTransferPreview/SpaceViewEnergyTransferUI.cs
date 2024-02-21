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

namespace ArgusLabs.DF.UI.GameplayHUD.SpaceView.EnergyTransferPreview
{
    public class SpaceViewEnergyTransferUI : MonoBehaviour
    {
        [SerializeField] protected TextMeshProUGUI _text_Time;
        [SerializeField] protected TextMeshProUGUI _text_EnergyAmount;
        [SerializeField] protected RectTransform _rotatingAnchor;
        [SerializeField] protected RectTransform _rotatingPanel;

        [SerializeField] protected GameObject _energyAmount;
        [SerializeField] protected GameObject _separator;

        protected EventManager _eventManager;

        protected ulong _energyTransferId = 0;
        protected Transform _canvasScale;

        protected bool _isListeningEvents;

        public void Awake() => Debug.Log($"SpaceViewEnergyTransferPreview -> Awake Frame: {Time.frameCount}");
        public void Start() => Debug.Log($"SpaceViewEnergyTransferPreview -> Start Frame: {Time.frameCount}");

        public void Init(EventManager eventManager, Transform canvasScale)
        {
            Debug.Log($"SpaceViewEnergyTransferPreview -> Init Frame: {Time.frameCount}");
            _eventManager = eventManager;
            _canvasScale = canvasScale;
            GetComponent<RectTransform>().anchorMin = Vector3.zero;
            GetComponent<RectTransform>().anchorMax = Vector3.zero;
            _isListeningEvents = false;
            ListenEvents();
        }

        /// <summary>
        /// this is only called by the ongoing ship info, not by the preview
        /// </summary>
        public virtual void Show(ClientEnergyTransfer transfer, Vector2 position, float rotation)
        {
            Debug.Log($"SpaceViewEnergyTransferPreview -> Show Frame: {Time.frameCount}");
            gameObject.SetActive(true);
            _energyTransferId = transfer.Id;
            SetTime(TimeSpan.FromSeconds(transfer.TravelDuration), "Landing");
            SetEnergyAmount(transfer.EnergyOnEmbark);
            position = position / _canvasScale.localScale.x;
            GetComponent<RectTransform>().anchoredPosition = position;
        }

        private void OnEnable()
        {
            Debug.Log($"SpaceViewEnergyTransferPreview -> OnEnable Frame: {Time.frameCount}");
            ListenEvents();
        }

        protected virtual void ListenEvents() { _isListeningEvents = true; }

        private void OnDisable()
        {
            Debug.Log($"SpaceViewEnergyTransferPreview -> OnDisable Frame: {Time.frameCount}");
            ListenEvents();
        }

        protected virtual void UnlistenEvents() { _isListeningEvents = false; }


        protected virtual void SetTime(TimeSpan duration, string defaultText = "")
        {
            string timeText = UIUtility.TimeSpanToString(duration);
            _text_Time.text = string.IsNullOrEmpty(timeText) ? defaultText : timeText;
            _text_Time.color = Color.white;
        }

        protected void SetEnergyAmount(int amount)
        {
            _separator.SetActive(true);
            _energyAmount.SetActive(true);
            _text_EnergyAmount.text = amount.ToString();
        }
    }
}
