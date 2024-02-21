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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD
{

    public class WinnerPopupUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _personaText;
        [SerializeField] private Button _copyIdButton;
        [SerializeField] private TextMeshProUGUI _idText;
        [SerializeField] private Button _goToLinkButton;

        [SerializeField] private GameObject _copiedMessage;
        [SerializeField] private Button _okayButton;

        private Action _onButtonClick;

        private string _uid;
        private readonly string _url = "https://darkfrontier.gg/winner-form";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void CopyToClipboardAndShare(string textToCopy);
#endif

        public void Show(Player player, Action OnButtonClick = null)
        {
            gameObject.SetActive(true);
            _onButtonClick = OnButtonClick;

            _personaText.text = $"Congratulations {player.PersonaTag}\r\nyou are among the top 10 players!";
            _idText.text = player.Uid;
            _uid = player.Uid;
            _copiedMessage.SetActive(false);
        }

        private void CopyUid()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CopyToClipboardAndShare(_uid);
#else
            GUIUtility.systemCopyBuffer = _uid;
#endif
            _copiedMessage.SetActive(true);
        }

        private void GoToLink()
        {
            Application.OpenURL(_url);
        }

        private void OnEnable()
        {
            _okayButton.onClick.AddListener(() => {
                _onButtonClick?.Invoke();
                gameObject.SetActive(false);
            });
            _copyIdButton.onClick.AddListener(CopyUid);
            _goToLinkButton.onClick.AddListener(GoToLink);
        }

        private void OnDisable()
        {
            _okayButton.onClick.RemoveAllListeners();
            _copyIdButton.onClick.RemoveAllListeners();
            _goToLinkButton.onClick.RemoveAllListeners();
        }

    }
}
