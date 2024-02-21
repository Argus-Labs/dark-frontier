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
using ArgusLabs.DF.UI.FirstForms;
using ArgusLabs.DF.UI.GameplayHUD;
using ArgusLabs.DF.UI.GameplayHUD.InGameMenu;
using UnityEngine;

namespace ArgusLabs.DF.UI
{
    public class RootUIManager : MonoBehaviour
    {
        [SerializeField] private PreGameUIManager preGameBackground;
        [SerializeField] private PreGameUIManager preGameUI;
        [SerializeField] private PopupUIManager popupUI;
        [SerializeField] private CursorIconSwitcher cursorController;
        [SerializeField] private SplashScreenUIManager splashScreenUI;
        [SerializeField] private KeyAndTagInputUIManager keyAndTagInputUI;
        [SerializeField] private LoadingScreenUIManager loadingScreenUI;
        [SerializeField] private GameplayUIManager gameplayUI;
        [SerializeField] private TutorialUIManager tutorialUI;
        [SerializeField] private ControlsUIManager controlsUI;
        [SerializeField] private LinkedEvmListUIManager linkedEvmListUI;
        [SerializeField] private LinkNewEvmUIManager linkNewEvmUI;

        private bool _showTutorialAtGameStart;

        public void Init(EventManager eventManager)
        {
            preGameBackground.Init(eventManager.MainMenuEvents);
            preGameUI.Init(eventManager.MainMenuEvents);
            splashScreenUI.Init(eventManager.MainMenuEvents);
            loadingScreenUI.Init(eventManager);
            keyAndTagInputUI.Init(eventManager);
            cursorController.Init(eventManager);
            gameplayUI.Init(eventManager);
            tutorialUI.Init(eventManager);
            controlsUI.Init(eventManager);
            popupUI.Init(eventManager);
            linkedEvmListUI.Init(eventManager);
            linkNewEvmUI.Init(eventManager);
            eventManager.GeneralEvents.LoadingStarted += (showFindingHomeMsg) =>
            {
                _showTutorialAtGameStart = showFindingHomeMsg; //showFindingHomeMsg is true only on first time playing, so we use it to determine showing tutorial at start
                loadingScreenUI.Show(showFindingHomeMsg);
            };
            eventManager.GeneralEvents.OpenTutorial += () => tutorialUI.gameObject.SetActive(true);
            eventManager.GeneralEvents.OpenControls += () => controlsUI.gameObject.SetActive(true);
            eventManager.GeneralEvents.OpenLinkedEvmList += () => linkedEvmListUI.gameObject.SetActive(true);
            eventManager.GeneralEvents.OpenLinkNewEvm += () => linkNewEvmUI.gameObject.SetActive(true);
            eventManager.MainMenuEvents.SplashScreenOpen += () =>
            {
                preGameUI.gameObject.SetActive(true);
                splashScreenUI.gameObject.SetActive(true);
            };
            eventManager.MainMenuEvents.KeyAndTagScreenOpen += () => keyAndTagInputUI.gameObject.SetActive(true);
            eventManager.GameplayEvents.GameplayEntered += () =>
            {
                gameplayUI.gameObject.SetActive(true);
                if (_showTutorialAtGameStart) tutorialUI.gameObject.SetActive(true);
            };
        }
    }
}
