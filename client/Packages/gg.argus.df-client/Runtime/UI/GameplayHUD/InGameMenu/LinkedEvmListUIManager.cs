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

using System.Collections.Generic;
using ArgusLabs.DF.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD.InGameMenu
{
    public class LinkedEvmListUIManager : MonoBehaviour
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private RectTransform _entryRoot;
        [SerializeField] private LinkedEvmListEntry _listEntryPrefab;

        private EventManager _eventManager;
        private List<LinkedEvmListEntry> _linkedEvmList;

        public void Init(EventManager eventManager)
        {
            _eventManager = eventManager;

            _linkedEvmList = new List<LinkedEvmListEntry>();
        }

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(Close);
            _eventManager.GeneralEvents.RespondLinkedEvmList += RefreshList;
            _eventManager.GeneralEvents?.RequestLinkedEvmList?.Invoke();
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveAllListeners();
            _eventManager.GeneralEvents.RespondLinkedEvmList -= RefreshList;
        }

        private void Close()
        {
            _eventManager.GeneralEvents.InGameSubmenuClosed?.Invoke();
            gameObject.SetActive(false);
        }

        private void RefreshList(string[] linkedEvmData)
        {
            for (int i = 0; i < linkedEvmData.Length; i++)
            {
                if (_linkedEvmList.Count-1 < i)
                {
                    LinkedEvmListEntry newEntry = Instantiate(_listEntryPrefab, _entryRoot);
                    _linkedEvmList.Add(newEntry);
                }

                _linkedEvmList[i].SetAddress(linkedEvmData[i]);
            }
        }
    }
}
