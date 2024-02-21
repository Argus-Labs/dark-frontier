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
using System.Collections.Generic;
using ArgusLabs.DF.Core;
using ArgusLabs.DF.UI.Utilities;
using ArgusLabs.WorldEngineClient.Communications.Rpc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD.Leaderboard
{
    public class LeaderboardUIManager : MonoBehaviour
    {
        [SerializeField] private PlayerLeaderStatUI _playerLeaderStat;
        [SerializeField] private TextMeshProUGUI _instanceName;
        [SerializeField] private TextMeshProUGUI _instanceTimer;
        [SerializeField] private Button _toggleLeaderboardButtonTop;
        [SerializeField] private Button _toggleLeaderboardButtonBottom;
        [SerializeField] private Animator _controller;
        [SerializeField] private LeaderboardEntryUI _entryPrefab;
        [SerializeField] private RectTransform _entryRoot;
        [SerializeField] private PlayerLeaderStatUI _playerStat;
        [SerializeField] private Sprite[] _topRanksHighlight;
        [SerializeField] private Sprite _playerHighlight;
        [SerializeField] private GameObject _loadingNotif;

        private bool _leaderboardOpened;
        private List<LeaderboardEntryUI> _entries;
        private EventManager _eventManager;

        public void Init(EventManager eventManager)
        {
            _eventManager = eventManager;
            _playerLeaderStat.Init(eventManager);
            _entries = new List<LeaderboardEntryUI>();
            _playerStat.Init(eventManager);
            _leaderboardOpened = false;
        }

        private void OnEnable()
        {
            _toggleLeaderboardButtonTop.onClick.AddListener(ToggleLeaderboard);
            _toggleLeaderboardButtonBottom.onClick.AddListener(ToggleLeaderboard);

            if (_eventManager != null)
            {
                _eventManager.GameplayEvents.InstanceNameUpdated += OnInstanceNameUpdated;
                _eventManager.GameplayEvents.InstanceTimerUpdated += OnInstanceTimerUpdated;
            }
        }

        private void OnDisable()
        {
            _toggleLeaderboardButtonTop.onClick.RemoveAllListeners();
            _toggleLeaderboardButtonBottom.onClick.RemoveAllListeners();

            if (_eventManager != null)
            {
                _eventManager.GameplayEvents.InstanceNameUpdated -= OnInstanceNameUpdated;
                _eventManager.GameplayEvents.InstanceTimerUpdated -= OnInstanceTimerUpdated;
            }
        }

        private void OnInstanceNameUpdated(string text)
        {
            _instanceName.text = text.ToUpper();
        }

        private void OnInstanceTimerUpdated(double remainingInstanceTime)
        {
            if (remainingInstanceTime > 0)
                _instanceTimer.text = UIUtility.TimeSpanToClockString(TimeSpan.FromSeconds(remainingInstanceTime));
            else
                _instanceTimer.text = "ENDED";
        }

        private void ToggleLeaderboard()
        {
            if (_leaderboardOpened)
            {
                _controller.SetBool("OpenCloseToggle", false);
                _eventManager.GameplayEvents.LeaderboardUpdated -= OnLeaderboardUpdated;
                _eventManager.GameplayEvents.LeaderboardClosed?.Invoke();
                _leaderboardOpened = false;
            }
            else
            {
                if (_entries.Count == 0)
                {
                    _loadingNotif.SetActive(true);
                }
                _controller.SetBool("OpenCloseToggle", true);
                _eventManager.GameplayEvents.LeaderboardUpdated += OnLeaderboardUpdated;
                _eventManager.GameplayEvents.LeaderboardOpened?.Invoke();
                _leaderboardOpened = true;
            }
        }

        private void OnLeaderboardUpdated(LeaderboardReply reply, string playerTag)
        {
            _loadingNotif.SetActive(false);
            
            for (int i = 0; i < reply.Entries.Length; i++)
            {
                LeaderboardEntry entry = reply.Entries[i];
                
                if (string.IsNullOrEmpty(entry.personaTag))
                    continue;
                
                if (i >= _entries.Count)
                {
                    LeaderboardEntryUI newEntry = Instantiate(_entryPrefab, _entryRoot);
                    _entries.Add(newEntry);
                }

                if (i < 3)
                {
                    // Why not just pass an entry?
                    _entries[i].SetData(entry.rank, entry.personaTag, entry.score, _topRanksHighlight[i]);
                }
                else if (entry.personaTag == playerTag)
                {
                    _entries[i].SetData(entry.rank, entry.personaTag, entry.score, _playerHighlight);
                }
                else
                {
                    _entries[i].SetData(entry.rank, entry.personaTag, entry.score, null);
                }
            }
        }
    }
}
