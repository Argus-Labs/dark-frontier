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
using ArgusLabs.DF.UI.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD
{
    public class ExplorerControlUIManager : MonoBehaviour
    {
        [SerializeField] private Button _button_StartExplore;
        [SerializeField] private Button _button_StopExplore;
        [SerializeField] private Button _button_StartRelocate;
        [SerializeField] private Button _button_StopRelocate;
        [SerializeField] private MagicCircleAutoRotate _rotatingCircle;

        private GameplayEvents _eventManager;

        public void Init(GameplayEvents eventManager)
        {
            _eventManager = eventManager;
        }

        private void Start()
        {
        }

        private void OnEnable()
        {
            _button_StartRelocate.onClick.AddListener(StartRelocate);
            _button_StopRelocate.onClick.AddListener(StopRelocate);
            _button_StartExplore.onClick.AddListener(StartExplore);
            _button_StopExplore.onClick.AddListener(StopExplore);

            if (_eventManager != null)
            {
                _eventManager.ExplorerMoveStateChanged += OnExplorerMoveStateChanged;
                _eventManager.RelocatingStateChanged += OnRelocatingStateChanged;
            }
        }

        private void OnDisable()
        {
            _button_StartRelocate.onClick.RemoveAllListeners();
            _button_StopRelocate.onClick.RemoveAllListeners();
            _button_StartExplore.onClick.RemoveAllListeners();
            _button_StopExplore.onClick.RemoveAllListeners();

            if (_eventManager != null)
            {
                _eventManager.ExplorerMoveStateChanged -= OnExplorerMoveStateChanged;
                _eventManager.RelocatingStateChanged -= OnRelocatingStateChanged;
            }
        }

        private void StartExplore()
        {
            _eventManager.ExplorerStarting?.Invoke();
        }

        private void StopExplore()
        {
            _eventManager.ExplorerStopping?.Invoke();
        }

        private void StartRelocate()
        {
            _eventManager.ExplorerStartRelocating?.Invoke();
        }

        private void StopRelocate()
        {
            _eventManager.ExplorerStopRelocating?.Invoke();
        }

        private void OnRelocatingStateChanged(bool in_Status)
        {
            if (in_Status)
            {
                _button_StartRelocate.gameObject.SetActive(false);
                _button_StopRelocate.gameObject.SetActive(true);
                SendMessageUpwards("OnStartRelocating");
            }
            else
            {
                _button_StartRelocate.gameObject.SetActive(true);
                _button_StopRelocate.gameObject.SetActive(false);
                SendMessageUpwards("OnStopRelocating");
            }
        }

        private void OnExplorerMoveStateChanged(bool in_Status)
        {
            if (in_Status)
            {
                _button_StartExplore.gameObject.SetActive(false);
                _button_StopExplore.gameObject.SetActive(true);
                _rotatingCircle.Start();
            }
            else
            {
                _button_StartExplore.gameObject.SetActive(true);
                _button_StopExplore.gameObject.SetActive(false);
                _rotatingCircle.Stop();
            }
        }
    }
}
