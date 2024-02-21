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

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD.InGameMenu
{

    public class LinkedEvmListEntry : MonoBehaviour, IPointerExitHandler
    {
        [SerializeField] private Button _copyButton;
        [SerializeField] private TextMeshProUGUI _addressText;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void CopyToClipboardAndShare(string textToCopy);
#endif

        private string _address;
        private bool _isCopyText;

        private void OnEnable()
        {
            _copyButton.onClick.AddListener(CopyAddress);
            _isCopyText = false;
        }

        private void CopyAddress()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CopyToClipboardAndShare(_address);
#else
            GUIUtility.systemCopyBuffer = _address;
#endif

            _addressText.text = "COPIED TO CLIPBOARD";
            _isCopyText = true;
        }

        public void SetAddress(string address)
        {
            _address = address;
            _addressText.text = address;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (EventSystem.current.currentSelectedGameObject == gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            if (_isCopyText)
            {
                _addressText.text = _address;
                _isCopyText = false;
            }
        }
    }
}
