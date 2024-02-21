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

namespace ArgusLabs.DF.UI
{
    public class LoadingScreenUIManager : MonoBehaviour
    {
        [SerializeField] private GameObject _findingHomeTexts;

        private EventManager _eventManager;
        private Animator _animator;

        public void Init(EventManager eventManager)
        {
            _eventManager = eventManager;
            _animator = GetComponent<Animator>();
        }

        public void Show(bool showFindingHomeMessage)
        {
            gameObject.SetActive(true);
            _findingHomeTexts.SetActive(showFindingHomeMessage);
        }

        private void OnEnable()
        {
            if (_eventManager != null)
            {
                _eventManager.GeneralEvents.LoadingFinished += OnLoadingFinish;
            }
        }

        private void OnDisable()
        {
            if (_eventManager != null)
            {
                _eventManager.GeneralEvents.LoadingFinished -= OnLoadingFinish;
            }
        }

        private void OnLoadingFinish()
        {
            _animator.SetTrigger("LoadingFinish");
        }

        public void CloseLoadingScreen()
        {
            _eventManager.MainMenuEvents.SplashScreenClose?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
