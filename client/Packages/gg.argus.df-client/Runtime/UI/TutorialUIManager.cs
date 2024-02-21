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
    public class TutorialUIManager : MonoBehaviour
    {
        [SerializeField] private Button _button_StartTutorial;
        [SerializeField] private Button _button_SkipTutorial;
        [SerializeField] private Button _button_FinishTutorial;
        [SerializeField] private Button _button_Close;
        [SerializeField] private Button _button_PreviousPage;
        [SerializeField] private Button _button_Replay;
        [SerializeField] private Button _button_NextPage;

        [SerializeField] private Image[] _pageLines;
        [SerializeField] private Sprite _activePageLine;
        [SerializeField] private Sprite _inactivePageLine;

        [SerializeField] private GameObject _navigation;
        [SerializeField] private GameObject[] _tutorialContents;
        [SerializeField] private TutorialVideoController[] _videoPlayers;

        private int _totalPages;        // Total amount of pages starting at 1
        private int _currentPage;       // Current page being viewed starting at 0

        private EventManager _eventManager;

        public void Init(EventManager eventManager)
        {
            _eventManager = eventManager;
        }

        private void Start()
        {
            _totalPages = _tutorialContents.Length;
            _videoPlayers[0].SetUrl($"file://{Application.streamingAssetsPath}/tutorial-1.mp4");
            _videoPlayers[1].SetUrl($"file://{Application.streamingAssetsPath}/tutorial-2.mp4");
            _videoPlayers[2].SetUrl($"file://{Application.streamingAssetsPath}/tutorial-3.mp4");
        }

        private void OnEnable()
        {
            _currentPage = 0;
            RefreshPage();
            _button_StartTutorial.onClick.AddListener(NextPage);
            _button_SkipTutorial.onClick.AddListener(CloseTutorial);
            _button_FinishTutorial.onClick.AddListener(CloseTutorial);
            _button_Close.onClick.AddListener(CloseTutorial);
            _button_PreviousPage.onClick.AddListener(PreviousPage);
            _button_NextPage.onClick.AddListener(NextPage);
        }

        private void OnDisable()
        {
            _button_StartTutorial.onClick.RemoveAllListeners();
            _button_FinishTutorial.onClick.RemoveAllListeners();
            _button_Close.onClick.RemoveAllListeners();
            _button_SkipTutorial.onClick.RemoveAllListeners();
            _button_PreviousPage.onClick.RemoveAllListeners();
            _button_NextPage.onClick.RemoveAllListeners();
        }

        private void PreviousPage()
        {
            _currentPage = Mathf.Clamp(_currentPage - 1, 0, _totalPages - 1);
            RefreshPage();
        }

        private void NextPage()
        {
            _currentPage = Mathf.Clamp(_currentPage + 1, 0, _totalPages - 1);
            RefreshPage();
        }

        private void CloseTutorial()
        {
            _eventManager.GeneralEvents.InGameSubmenuClosed?.Invoke();
            gameObject.SetActive(false);
        }

        private void RefreshPage()
        {
            for (int i = 0; i < _tutorialContents.Length; i++)
            {
                _tutorialContents[i].SetActive(i == _currentPage);
            }
            if (_currentPage == 0)
            {
                _navigation.SetActive(false);
            }
            else
            {   
                _navigation.SetActive(true);
                if (_currentPage == _totalPages - 1)
                    _button_FinishTutorial.gameObject.SetActive(true);
                else
                    _button_FinishTutorial.gameObject.SetActive(false);
                int pageAfterFirst = _currentPage - 1;
                SetPage(pageAfterFirst);
                _videoPlayers[pageAfterFirst].Prepare();
                _button_Replay.gameObject.SetActive(false);
            }

            _button_PreviousPage.gameObject.SetActive(_currentPage > 0);
            _button_NextPage.gameObject.SetActive(_currentPage < _totalPages - 1);
        }

        private void SetPage(int currentPage)
        {
            for (int i = 0; i < _pageLines.Length; i++)
            {
                if (i == currentPage)
                {
                    _pageLines[i].sprite = _activePageLine;
                }
                else
                {
                    _pageLines[i].sprite = _inactivePageLine;
                }
            }
        }
    }
}
