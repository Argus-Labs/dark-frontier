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
using ArgusLabs.DF.Core.Space;
using UnityEngine;

namespace ArgusLabs.DF.UI
{
    public class CursorIconSwitcher : MonoBehaviour
    {
        [SerializeField] private Texture2D defaultCursor;
        [SerializeField] private Texture2D clickableCursor;
        [SerializeField] private Texture2D relocateCursor;

        private bool _isRelocating;
        private EventManager _eventManager;

        private void Awake()
        {
            _isRelocating = false;
            Cursor.SetCursor(defaultCursor, new Vector2(defaultCursor.width * 0.33f, defaultCursor.height * 0.33f), CursorMode.Auto);
        }
        public void Init(EventManager eventManager)
        {
            _eventManager = eventManager;

            _eventManager.GameplayEvents.RelocatingStateChanged += OnRelocatingStateChanged;
            _eventManager.GameplayEvents.PlanetHoverStarted += OnPlanetHoverStarted;
            _eventManager.GameplayEvents.PlanetHoverEnded += OnPlanetHoverEnded;
        }

        private void OnRelocatingStateChanged(bool obj)
        {
            if (obj)
            {
                OnStartRelocating();
            }
            else
            {
                OnStopRelocating();
            }
        }

        private void OnPlanetHoverStarted(Planet arg1, bool arg2)
        {
            OnEnterClickable();
        }

        private void OnPlanetHoverEnded()
        {
            OnExitClickable();
        }

        // to be called through SendMessageUpward from one of the UI components
        public void OnEnterClickable()
        {
            if (!_isRelocating)
            {
                Cursor.SetCursor(clickableCursor, new Vector2(clickableCursor.width * 0.33f, clickableCursor.height * 0.33f), CursorMode.Auto);
            }
        }

        // to be called through SendMessageUpward from one of the UI components
        public void OnExitClickable()
        {
            if (!_isRelocating)
            {
                Cursor.SetCursor(defaultCursor, new Vector2(defaultCursor.width * 0.33f, defaultCursor.height * 0.33f), CursorMode.Auto);
            }
        }

        public void OnStopRelocating()
        {
            _isRelocating = false;
            Cursor.SetCursor(defaultCursor, new Vector2(defaultCursor.width * 0.33f, defaultCursor.height * 0.33f), CursorMode.Auto);
        }

        public void OnStartRelocating()
        {
            _isRelocating = true;
            Cursor.SetCursor(relocateCursor, new Vector2(relocateCursor.width * 0.54f, relocateCursor.height * 0.77f), CursorMode.Auto);
        }
    }
}
