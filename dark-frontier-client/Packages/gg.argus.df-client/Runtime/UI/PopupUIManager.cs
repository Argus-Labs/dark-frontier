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
using ArgusLabs.DF.UI.GameplayHUD;
using UnityEngine;

namespace ArgusLabs.DF.UI
{
    public class PopupUIManager : MonoBehaviour
    {
        [SerializeField] private MessageOnlyPopupUIManager messageOnlyPopupUI;
        [SerializeField] private OneButtonPopupUIManager oneButtonPopupUI;
        [SerializeField] private ConnectionErrorUIManager errorUI;
        [SerializeField] private WinnerPopupUI winnerPopupUI;

        public void Init(EventManager eventManager)
        {
            errorUI.Init(eventManager.GeneralEvents);
            eventManager.GeneralEvents.Disconnected += () => errorUI.gameObject.SetActive(true);
            eventManager.GeneralEvents.MessagePopupOpen += (message) => messageOnlyPopupUI.Show(message);
            eventManager.GeneralEvents.MessagePopupClose += () => messageOnlyPopupUI.gameObject.SetActive(false);
            eventManager.GeneralEvents.OneButtonPopup += (message, onClick) => oneButtonPopupUI.Show(message, onClick);
            eventManager.GeneralEvents.WinnerPopup += (player, onClick) => winnerPopupUI.Show(player, onClick);
            messageOnlyPopupUI.gameObject.SetActive(false);
            oneButtonPopupUI.gameObject.SetActive(false);
            errorUI.gameObject.SetActive(false);
            winnerPopupUI.gameObject.SetActive(false);
        }
    }
}
