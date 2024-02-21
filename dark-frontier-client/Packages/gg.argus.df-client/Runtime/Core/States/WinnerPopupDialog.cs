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
using Smonch.CyclopsFramework;
using UnityEngine;

namespace ArgusLabs.DF.Core.States
{
    public class WinnerPopupDialog : CyclopsGameState
    {
        private readonly EventManager _eventManager;
        private readonly Action _exitCallback;
        private readonly Player _player;
        
        public WinnerPopupDialog(EventManager eventManager, Player player, Action exitCallback)
        {
            _eventManager = eventManager;
            _exitCallback = exitCallback;
            _player = player;
        }
        
        protected override void OnEnter()
        {
            Debug.Log($"{nameof(WinnerPopupDialog)} {nameof(OnEnter)}");
            
            _eventManager.GeneralEvents.WinnerPopup?.Invoke(_player, () =>
            {
                _exitCallback?.Invoke();
                Stop();
            });
        }

        protected override void OnExit()
        {
            Debug.Log($"{nameof(Loader)} {nameof(OnExit)}");
        }
    }
}