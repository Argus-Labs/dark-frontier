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

using ArgusLabs.DF.UI.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD.Leaderboard
{
    public class LeaderboardEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _rankText;
        [SerializeField] private TextMeshProUGUI _playerTagText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private Image _highlightBackground;

        [SerializeField] private bool _addPrefixToRank;

        public void SetData(int rank, string playerTag, int score, Sprite highlight, bool isPlayer = false)
        {
            _rankText.text = _addPrefixToRank ? $"RANK {rank.ToString()}" : rank.ToString();
            _playerTagText.text = UIUtility.TrimLongString(playerTag, 16);
            _scoreText.text = score.ToString();
            if (_highlightBackground != null)
            {
                if (highlight != null)
                {
                    _highlightBackground.gameObject.SetActive(true);
                    _highlightBackground.sprite = highlight;
                }
                else
                {
                    _highlightBackground.gameObject.SetActive(false);
                }
            }
        }
    }
}
