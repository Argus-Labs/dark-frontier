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
using System.Text.RegularExpressions;
using ArgusLabs.DF.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.FirstForms
{
    public class InputFormUIManager : MonoBehaviour
    {
        [SerializeField] protected TMP_InputField _inputField;
        [SerializeField] protected Button _submitButton;

        private Action<string> _onSubmit;

        protected EventManager _eventManager;
        
        public void Init(EventManager eventManager)
        {
            _eventManager = eventManager;
        }

        public void Show(string regexPattern, Action<string> onSubmit)
        {
            _inputField.onValidateInput = (string text, int index, char c) => Regex.IsMatch(c.ToString(), regexPattern) ? c : (char)0;
            _onSubmit = onSubmit;
            gameObject.SetActive(true);
        }

        private void Submit()
        {
            _onSubmit.Invoke(_inputField.text.ToUpper());
        }

        private void OnEnable()
        {
            if (_eventManager != null)
            {
                _submitButton.onClick.AddListener(Submit);

                _eventManager.MainMenuEvents.KeyAndTagMessageOpen += OnKeyAndTagMessageOpen;
                _eventManager.MainMenuEvents.KeyAndTagMessageClose += OnKeyAndTagMessageClose;
            }
        }

        private void OnKeyAndTagMessageClose()
        {
            _submitButton.interactable = true;
        }

        private void OnKeyAndTagMessageOpen(string obj)
        {
            _submitButton.interactable = false;
        }

        private void OnDisable()
        {
            if (_eventManager != null)
            {
                _submitButton.onClick.RemoveListener(Submit);
                _onSubmit = null;
            }
        }

    }
}
