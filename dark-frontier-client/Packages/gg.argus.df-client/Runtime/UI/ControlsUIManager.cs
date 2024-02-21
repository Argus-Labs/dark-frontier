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
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI
{
    public class ControlsUIManager : MonoBehaviour
    {
        [SerializeField] private Button _button_Close;
        private EventManager _eventManager;

        public void Init(EventManager eventManager)
        {
            _eventManager = eventManager;
        }

        private void OnEnable()
        {
            _button_Close.onClick.AddListener(CloseControl);
        }

        private void OnDisable()
        {
            _button_Close.onClick.RemoveAllListeners();
        }


        private void CloseControl()
        {
            _eventManager.GeneralEvents.InGameSubmenuClosed?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
