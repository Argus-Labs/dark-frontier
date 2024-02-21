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

namespace ArgusLabs.DF.UI
{
    public class OneButtonPopupUIManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _message;

        [SerializeField] private Button _okayButton;

        private Action _onButtonClick;

        public void Show(string message, Action OnButtonClick = null)
        {
            gameObject.SetActive(true);
            _message.text = message;
            _onButtonClick = OnButtonClick;
        }

        private void OnEnable()
        {
            _okayButton.onClick.AddListener(() => {
                _onButtonClick?.Invoke();
                gameObject.SetActive(false);
            });
        }

        private void OnDisable()
        {
            _okayButton.onClick.RemoveAllListeners();
        }
    }
}
