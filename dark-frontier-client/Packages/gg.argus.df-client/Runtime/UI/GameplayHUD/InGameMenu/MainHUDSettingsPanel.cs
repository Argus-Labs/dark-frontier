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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD.InGameMenu
{
    public class MainHUDSettingsPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text_UserID;

        [SerializeField] private GameObject _blocker;
        [SerializeField] private GameObject _menuPanel;
        [SerializeField] private Button _button_Continue;
        [SerializeField] private Button _button_HowToPlay;
        [SerializeField] private Button _button_Controls;
        [SerializeField] private Button _button_OpenLinkedEvm;
        [SerializeField] private Button _button_LinkNewEvm;
        [SerializeField] private TextMeshProUGUI _playerTag;

        private GeneralEvents _eventManager;

        public void Init(GeneralEvents eventManager)
        {
            _eventManager = eventManager;
        }

        private void OnEnable()
        {
            if (_eventManager != null)
            {
                _button_Continue.onClick.AddListener(CloseSettings);
                _button_HowToPlay.onClick.AddListener(OpenHowToPlay);
                _button_Controls.onClick.AddListener(OpenControls);
                _button_OpenLinkedEvm.onClick.AddListener(OpenLinkedEvmList);
                _button_LinkNewEvm.onClick.AddListener(OpenLinkNewEvm);

                _eventManager.RespondPlayerTag += OnRespondPlayerTag;

                _eventManager.RequestPlayerTag?.Invoke();
            }
        }

        private void OnDisable()
        {
            if (_eventManager != null)
            {
                _button_Continue.onClick.RemoveAllListeners();
                _button_HowToPlay.onClick.RemoveAllListeners();
                _button_Controls.onClick.RemoveAllListeners();
                _button_OpenLinkedEvm.onClick.RemoveAllListeners();
                _button_LinkNewEvm.onClick.RemoveAllListeners();

                _eventManager.RespondPlayerTag -= OnRespondPlayerTag;
            }
        }

        private void CloseSettings()
        {
            gameObject.SetActive(false);
        }

        private void OpenHowToPlay()
        {
            // Open tutorial menu
            _eventManager.OpenTutorial?.Invoke();
            CloseForSubMenu();
        }

        private void OpenControls()
        {
            // EOpen controls menu
            _eventManager.OpenControls?.Invoke();
            CloseForSubMenu();
        }

        private void OpenLinkedEvmList()
        {
            _eventManager.OpenLinkedEvmList?.Invoke();
            CloseForSubMenu();
        }

        private void OpenLinkNewEvm()
        {
            _eventManager.OpenLinkNewEvm?.Invoke();
            CloseForSubMenu();
        }

        private void CloseForSubMenu()
        {
            _menuPanel.gameObject.SetActive(false);
            _blocker.SetActive(false);
            _eventManager.InGameSubmenuClosed += ReopenMenu;
        }

        private void ReopenMenu()
        {
            _menuPanel.gameObject.SetActive(true);
            _blocker.SetActive(true);
            _eventManager.InGameSubmenuClosed -= ReopenMenu;
        }

        private void OnRespondPlayerTag(string playerTag)
        {
            _playerTag.text = UIUtility.TrimLongString(playerTag, 15);
        }

        public void SetUserID(string in_UserID)
        {
            _text_UserID.text = in_UserID;
        }
    }
}
