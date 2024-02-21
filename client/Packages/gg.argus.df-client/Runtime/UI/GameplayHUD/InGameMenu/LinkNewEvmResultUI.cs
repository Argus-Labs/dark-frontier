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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD.InGameMenu
{
    public class LinkNewEvmResultUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _buttonText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _acceptButton;

        private Action _onClosed;

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(Close);
            _acceptButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveAllListeners();
            _acceptButton.onClick.RemoveAllListeners();
        }

        public void Show(bool isSuccess, Action onClosed)
        {
            gameObject.SetActive(true);
            if(isSuccess)
            {
                _resultText.text = "SUCCESS";
                _descriptionText.text = "Your account was successfully linked to this EVM Address";
                _buttonText.text = "LINK";
            }
            else
            {
                _resultText.text = "FAILED";
                _descriptionText.text = "We couldn't link your account to this EVM Address";
                _buttonText.text = "OK";
            }
            _onClosed = onClosed;
        }

        private void Close()
        {
            _onClosed?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
