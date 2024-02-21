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

namespace ArgusLabs.DF.UI
{
    public class ConnectionErrorUIManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text_ErrorMessage;

        [SerializeField] private Button _button_Retry;

        private GeneralEvents _eventManager;
        public void Init(GeneralEvents eventManager)
        {
            _eventManager = eventManager;
        }

        private void OnEnable()
        {
            _button_Retry.onClick.AddListener(Retry);
        }

        private void OnDisable()
        {
            _button_Retry.onClick.RemoveAllListeners();
        }


        private void Retry()
        {
            _eventManager.RetryConnection?.Invoke();
            gameObject.SetActive(false);
        }

        public void SetErrorMessage(string in_Message)
        {
            _text_ErrorMessage.text = in_Message;
        }
    }
}
