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

namespace ArgusLabs.DF.UI.FirstForms
{
    public class KeyAndTagInputUIManager : MonoBehaviour
    {
        [SerializeField] private SectionIcon _betaKeySection;
        [SerializeField] private SectionIcon _claimPersonaSection;
        [SerializeField] private SectionIcon _linkStartSection;
        [SerializeField] private InputFormUIManager _betaKeyForm;
        [SerializeField] private InputFormUIManager _playerTagForm;
        [SerializeField] private GameObject _linkStartPanel;
        [SerializeField] private GameObject _messagePanel;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Animator _animator;
        [SerializeField] private string _betaKeyValidationRegex = ".*";
        [SerializeField] private string _personaTagValidationRegex = "^[a-zA-Z0-9_]+$";
        
        private EventManager _eventManager;

        public void Init(EventManager eventManager)
        {
            _eventManager = eventManager;
            _betaKeyForm.Init(eventManager);
            _playerTagForm.Init(eventManager);
            _betaKeyForm.gameObject.SetActive(false);
            _playerTagForm.gameObject.SetActive(false);
            _linkStartPanel.gameObject.SetActive(false);
            _betaKeySection.SetInactive();
            _claimPersonaSection.SetInactive();
            _linkStartSection.SetInactive();
        }

        private void OnEnable()
        {
            if(_eventManager != null )
            {
                _eventManager.MainMenuEvents.BetaKeyEntryOpen += OnBetaKeyEntryOpen;
                _eventManager.MainMenuEvents.BetaKeyEntryClose += OnBetaKeyEntryClose;
                _eventManager.MainMenuEvents.PlayerTagEntryOpen += OnPlayerTagEntryOpen;
                _eventManager.MainMenuEvents.PlayerTagEntryClose += OnPlayerTagEntryClose;
                _eventManager.MainMenuEvents.LinkStartOpen += OnLinkStartOpen;
                _eventManager.MainMenuEvents.KeyAndTagMessageOpen += OnKeyAndTagMessageOpen;
                _eventManager.MainMenuEvents.KeyAndTagMessageClose += OnKeyAndTagMessageClose;
            }
        }

        private void OnDisable()
        {
            if(_eventManager != null) 
            {
                _eventManager.MainMenuEvents.BetaKeyEntryOpen -= OnBetaKeyEntryOpen;
                _eventManager.MainMenuEvents.BetaKeyEntryClose -= OnBetaKeyEntryClose;
                _eventManager.MainMenuEvents.PlayerTagEntryOpen -= OnPlayerTagEntryOpen;
                _eventManager.MainMenuEvents.PlayerTagEntryClose -= OnPlayerTagEntryClose;
                _eventManager.MainMenuEvents.LinkStartOpen -= OnLinkStartOpen;
                _eventManager.MainMenuEvents.KeyAndTagMessageOpen -= OnKeyAndTagMessageOpen;
                _eventManager.MainMenuEvents.KeyAndTagMessageClose -= OnKeyAndTagMessageClose;
            }
        }

        private void OnBetaKeyEntryOpen()
        {
            _betaKeyForm.Show(_betaKeyValidationRegex, (keyInput) => _eventManager.MainMenuEvents.BetaKeySubmitted?.Invoke(keyInput)); 
            _betaKeySection.SetActive();
        }

        private void OnBetaKeyEntryClose()
        {
            _betaKeyForm.gameObject.SetActive(false);
        }

        private void OnPlayerTagEntryOpen()
        {
            _playerTagForm.Show(_personaTagValidationRegex, (keyInput) => _eventManager.MainMenuEvents.PlayerTagSubmitted?.Invoke(keyInput));
            _betaKeySection.SetCompleted();
            _claimPersonaSection.SetActive();
        }

        private void OnPlayerTagEntryClose()
        {
            _playerTagForm.gameObject.SetActive(false);
        }

        private void OnLinkStartOpen()
        {
            _linkStartPanel.gameObject.SetActive(true);
            _animator.SetTrigger("LinkStart");
            _claimPersonaSection.SetCompleted();
            _linkStartSection.SetCompleted();
        }

        private void OnKeyAndTagMessageOpen(string message)
        {
            _messagePanel.SetActive(true);
            _messageText.text = message;
        }

        private void OnKeyAndTagMessageClose()
        {
            _messagePanel.SetActive(false);
        }

        public void CloseKeyAndTagScreen()
        {
            gameObject.SetActive(false);
            _eventManager.MainMenuEvents.KeyAndTagScreenClose?.Invoke();
        }
    }
}
