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

using System.Text;
using ArgusLabs.DF.Core;
using ArgusLabs.DF.Core.Space;
using ArgusLabs.DF.UI.Utilities;
using TMPro;
using UnityEngine;

namespace ArgusLabs.DF.UI.GameplayHUD
{
    public class HoveredGridInfo : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;

        private EventManager _eventManager;
        private StringBuilder _stringBuilder;

        public void Init(EventManager eventManager)
        {
            _eventManager = eventManager;
            _text.text = string.Empty;
            _stringBuilder = new StringBuilder();
        }

        private void OnEnable()
        {
            if (_eventManager != null)
            {
                _eventManager.GameplayEvents.GridHovered += OnGridHovered;
            }
        }

        private void OnDisable()
        {
            if (_eventManager != null)
            {
                _eventManager.GameplayEvents.GridHovered -= OnGridHovered;
            }
        }

        private void OnGridHovered(Sector sector)
        {
            _stringBuilder.Clear();
            
            if (!sector.WasExplored)
            {
                _stringBuilder.Append("unexplored ");
            }
            else
            {
                _stringBuilder.Append(UIUtility.SpaceAreaString(sector.Environment));
                _stringBuilder.Append(" ");
            }

            _stringBuilder.Append(sector.Position);
            _text.text = _stringBuilder.ToString();
        }
    }
}
