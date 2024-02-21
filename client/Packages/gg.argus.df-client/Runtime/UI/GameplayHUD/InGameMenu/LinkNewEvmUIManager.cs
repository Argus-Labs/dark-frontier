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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD.InGameMenu
{
    public class LinkNewEvmUIManager : MonoBehaviour
    {
        [SerializeField] private GameObject _linkInputPanel;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_InputField _addressInput;
        [SerializeField] private Button _linkButton;
        [SerializeField] private TextMeshProUGUI _linkButtonText;
        [SerializeField] private LinkNewEvmResultUI _resultUI;

        private EventManager _eventManager;

        public void Init(EventManager eventManager)
        {
            _eventManager = eventManager;
        }

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(Close);
            _linkButton.onClick.AddListener(LinkNewAddress);

            _eventManager.GeneralEvents.LinkNewEvmAddressCompleted += OnLinkNewEvmAddressCompleted;
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveAllListeners();
            _linkButton.onClick.RemoveAllListeners();

            _eventManager.GeneralEvents.LinkNewEvmAddressCompleted -= OnLinkNewEvmAddressCompleted;
        }

        private void OnLinkNewEvmAddressCompleted(bool obj)
        {
            _linkInputPanel.SetActive(false);
            _resultUI.Show(obj, () => {
                _linkInputPanel.SetActive(true);
                _linkButton.interactable = true;
                _linkButtonText.text = "LINK";
                _addressInput.interactable = true;
                _addressInput.text = "";
                _closeButton.interactable = true;
            });
        }

        private void LinkNewAddress()
        {
            _eventManager.GeneralEvents.LinkNewEvmAddress?.Invoke(_addressInput.text.ToLower());
            _linkButtonText.text = "LINKING...";
            _closeButton.interactable = false;
            _addressInput.interactable = false;
            _linkButton.interactable = false;
        }

        private void Close()
        {
            _eventManager.GeneralEvents.InGameSubmenuClosed?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
