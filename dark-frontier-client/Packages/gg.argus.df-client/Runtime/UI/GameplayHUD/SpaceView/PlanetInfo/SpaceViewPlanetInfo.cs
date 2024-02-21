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
using ArgusLabs.DF.Core.Space;
using ArgusLabs.DF.UI.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD.SpaceView.PlanetInfo
{
    public class SpaceViewPlanetInfo : MonoBehaviour
    {
        [SerializeField] private RectTransform _PlanetInfoFrame;

        [SerializeField] private GameObject _moreInfo;

        [SerializeField] private Button _button_InfoClose;
        [SerializeField] private Button _button_ShowInfo;

        [SerializeField] private TextMeshProUGUI _text_PlanetOwner;
        [SerializeField] private TextMeshProUGUI _text_PlanetName;
        [SerializeField] private TextMeshProUGUI _text_PlanetLevel;
        [SerializeField] private TextMeshProUGUI _text_PlanetEnergy;
        [SerializeField] private TextMeshProUGUI _text_PlanetEnergyMax;
        [SerializeField] private TextMeshProUGUI _text_PlanetEnergyGrowth;
        [SerializeField] private TextMeshProUGUI _text_PlanetDefense;
        [SerializeField] private TextMeshProUGUI _text_PlanetRange;
        [SerializeField] private TextMeshProUGUI _text_PlanetSpeed;

        [SerializeField] private float _frameSizeLessInfo;
        [SerializeField] private float _frameSizeMoreInfo;

        private Action<bool> MoreInfoShown;
        private Action PanelClosed;

        private bool _isShowingMore;    // Track if currently showing more info

        private void OnEnable()
        {
            _button_InfoClose.onClick.AddListener(ClosePlanetInfo);
            _button_ShowInfo.onClick.AddListener(ToggleMoreInfo);

            ShowLessInfo();
        }

        private void OnDisable()
        {
            _button_InfoClose.onClick.RemoveAllListeners();
            _button_ShowInfo.onClick.RemoveAllListeners();
        }

        public void Show(Planet obj, bool isShowingMoreInfo, Action<bool> OnMoreInfoShown, Action OnPanelClosed)
        {
            gameObject.SetActive(true);
            SetPlanetInfo("Owner", "Planet", obj.Level);
            SetPlanetCurrentEnergy(Mathf.RoundToInt((float)obj.EnergyLevel));
            SetPlanetStats(Mathf.RoundToInt((float)obj.EnergyCapacity), Mathf.RoundToInt((float)obj.EnergyRefillPeriod), obj.Speed, obj.FullRange, Mathf.RoundToInt((float)obj.Defense));
            
            _isShowingMore = isShowingMoreInfo;
            if (_isShowingMore) ShowMoreInfo();
            else ShowLessInfo();

            MoreInfoShown = OnMoreInfoShown;
            PanelClosed = OnPanelClosed;
        }

        private void ClosePlanetInfo()
        {
            PanelClosed?.Invoke();
        }

        private void ToggleMoreInfo()
        {
            _isShowingMore = !_isShowingMore;
            MoreInfoShown?.Invoke(_isShowingMore);
            if (_isShowingMore)
            {
                ShowMoreInfo();
            }
            else
            {
                ShowLessInfo();
            }
        }

        private void ShowLessInfo()
        {
            _button_ShowInfo.transform.localScale = Vector3.one;
            _PlanetInfoFrame.sizeDelta = new Vector2(_PlanetInfoFrame.sizeDelta.x, _frameSizeLessInfo);
            _moreInfo.SetActive(false);
        }

        private void ShowMoreInfo()
        {
            _button_ShowInfo.transform.localScale = -Vector3.one;
            _PlanetInfoFrame.sizeDelta = new Vector2(_PlanetInfoFrame.sizeDelta.x, _frameSizeMoreInfo);
            _moreInfo.SetActive(true);
        }

        public void SetPlanetInfo(string in_PlanetOwner, string in_PlanetName, int in_PlanetLevel)
        {
            _text_PlanetOwner.text = in_PlanetOwner;
            _text_PlanetName.text = in_PlanetName;
            _text_PlanetLevel.text = "LV." + in_PlanetLevel;
        }

        public void SetPlanetStats(int in_EnergyMax, int in_FillTime, double in_Speed, int in_Range, int in_Defense)
        {
            _text_PlanetEnergyMax.text = in_EnergyMax.ToString();
            _text_PlanetEnergyGrowth.text = UIUtility.TimeSpanToString(TimeSpan.FromSeconds(in_FillTime));
            _text_PlanetSpeed.text = in_Speed.ToString("F2");
            _text_PlanetRange.text = in_Range.ToString();
            _text_PlanetDefense.text = in_Defense.ToString();
        }

        public void SetPlanetCurrentEnergy(int in_CurrentEnergy)
        {
            _text_PlanetEnergy.text = in_CurrentEnergy.ToString();
        }
    }
}
