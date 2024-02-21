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
using ArgusLabs.WorldEngineClient.Communications.Rpc;
using UnityEngine;

namespace ArgusLabs.DF.UI.GameplayHUD.Leaderboard
{
    public class PlayerLeaderStatUI : MonoBehaviour
    {
        [SerializeField] private LeaderboardEntryUI _playerLeaderboardDataUI;

        private EventManager _eventManager;

        public void Init(EventManager eventManager)
        {
            _eventManager = eventManager;
        }

        private void OnEnable()
        {
            if(_eventManager != null)
            {
                _eventManager.GameplayEvents.PlayerLeaderStatUpdated += OnPlayerLeaderStatUpdated;
            }
        }
        private void OnDisable()
        {
            if (_eventManager != null)
            {
                _eventManager.GameplayEvents.PlayerLeaderStatUpdated -= OnPlayerLeaderStatUpdated;
            }
        }

        private void OnPlayerLeaderStatUpdated(PlayerRankReply reply, string playerTag)
        {
            _playerLeaderboardDataUI.SetData((int)reply.rank, playerTag, Mathf.RoundToInt((float)reply.score), null);
        }
    }
}
