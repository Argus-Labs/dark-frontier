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

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD.PlanetIndex
{
 
    //no longer used
    public class MainHUDPlanetIndexPageButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text_Label;
        [SerializeField] private Image _image_Frame;
        [SerializeField] private Toggle _toggle_PageButton;

        [SerializeField] private Color32 _color_Font_Selected;
        [SerializeField] private Color32 _color_Font_Regular;

        [SerializeField] private PlanetIndexUIManager _planetIndexPanel;

        public int Page { get; private set; }


        private void OnEnable()
        {
            _toggle_PageButton.onValueChanged.AddListener(SelectPage);
        }

        private void OnDisable()
        {
            _toggle_PageButton.onValueChanged.RemoveAllListeners();
        }


        private void SelectPage(bool in_Status)
        {
            if (in_Status)
            {
                _text_Label.color = _color_Font_Selected;
                _text_Label.fontSize = 26;
                _toggle_PageButton.interactable = false;

            }
            else
            {
                _text_Label.color = _color_Font_Regular;
                _text_Label.fontSize = 18;
                _toggle_PageButton.interactable = true;
            }
        }

        public void SetPage(int in_Page)
        {
            Page = in_Page;
            _text_Label.text = Page.ToString();
        }

        public void SetSelected(bool in_Status)
        {
            _toggle_PageButton.isOn = in_Status;
        }
    }
}
