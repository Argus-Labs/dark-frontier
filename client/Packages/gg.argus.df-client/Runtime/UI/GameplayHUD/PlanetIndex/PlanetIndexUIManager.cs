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

using System.Collections.Generic;
using ArgusLabs.DF.Core;
using ArgusLabs.DF.Core.Configs;
using ArgusLabs.DF.Core.Space;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD.PlanetIndex
{
    public class PlanetIndexUIManager : MonoBehaviour
    {
        [SerializeField] private Toggle _allPlanetsTabToggle;

        [SerializeField] private RectTransform _planetIndexEntryRoot;
        [SerializeField] private PlanetIndexEntryRoot _entryPrefab;

        [SerializeField] private LayoutElement _topFillerElement;
        [SerializeField] private LayoutElement _bottomFillerElement;
        [SerializeField] private RectTransform _contentRect;
        [SerializeField] private Scrollbar _scrollbar;
        [SerializeField] private VerticalLayoutGroup _contentVerticalLayout; //to get spacing for the correct _entryHeight
        [SerializeField] private RectTransform _viewport; //to get the number of shown entries
        private readonly int _entryOffset = 2; //entries that are still active before the first shown entry and after the last shown entry to be buffer
        private float _entryHeight;
        private int _shownEntries;
        private int _firstShownIndex;

        private struct PlanetAndSortingValue
        {
            public Planet Planet;
            public int SortingValue;
        }

        private List<PlanetIndexEntryRoot> _planetIndexEntries;
        private List<PlanetAndSortingValue> _planetList;
        private List<PlanetAndSortingValue> _activePlanetList;

        private GameplayEvents _eventManager;
        private Sprite[] _planetIcons;

        private string _selectedPlanet;         // Track if a planet is selected

        private bool _isShowingAll;

        public void Init(GameplayEvents eventManager, Sprite[] planetIcons)
        {
            _planetIndexEntries = new List<PlanetIndexEntryRoot>();
            _planetList = new List<PlanetAndSortingValue>();
            _activePlanetList = new List<PlanetAndSortingValue>();
            _planetIcons = planetIcons;
            _eventManager = eventManager;
            _isShowingAll = true;
            _entryHeight = _entryPrefab.GetComponent<LayoutElement>().preferredHeight + _contentVerticalLayout.spacing;
            _shownEntries = Mathf.FloorToInt(_viewport.sizeDelta.y / _entryHeight);
        }

        #region Event Setups
        private void OnEnable()
        {
            _allPlanetsTabToggle.onValueChanged.AddListener(SwitchTab);
            _scrollbar.onValueChanged.AddListener(OnScroll);
            if (_eventManager != null)
            {
                _eventManager.PlanetExportUpdated += OnPlanetExportUpdated;
                _eventManager.PlanetIncomingTransferUpdated += OnPlanetIncomingTransferUpdated;
                _eventManager.PlanetControlChanged += OnPlanetControlChanged;
                _eventManager.PlanetSelected += OnPlanetSelected;
                _eventManager.PlanetDeselected += OnPlanetDeselected;
            }
        }

        private void OnDisable()
        {
            _allPlanetsTabToggle.onValueChanged.RemoveAllListeners();

            if (_eventManager != null)
            {
                _eventManager.PlanetExportUpdated -= OnPlanetExportUpdated;
                _eventManager.PlanetIncomingTransferUpdated -= OnPlanetIncomingTransferUpdated;
                _eventManager.PlanetControlChanged -= OnPlanetControlChanged;
                _eventManager.PlanetSelected -= OnPlanetSelected;
                _eventManager.PlanetDeselected -= OnPlanetDeselected;
            }
        }

        private void OnScroll(float arg0)
        {
            int newFirstShownIndex = Mathf.FloorToInt(_contentRect.anchoredPosition.y / _entryHeight);
            if (newFirstShownIndex == _firstShownIndex) return;
            RefreshShownPlanetEntryList(_isShowingAll);
        }

        private void SwitchTab(bool toAllPlanet)
        {
            if (_isShowingAll == toAllPlanet) return;
            _isShowingAll = toAllPlanet;
            RefreshShownPlanetEntryList(_isShowingAll);
        }

        private void OnPlanetExportUpdated(Planet planet)
        {
            ActivePlanetUpdated(planet);
        }

        private void OnPlanetIncomingTransferUpdated(Planet planet)
        {
            ActivePlanetUpdated(planet);
        }

        private void ActivePlanetUpdated(Planet planet)
        {
            if (!planet.Owner.IsLocal) return;
            var activePlanetSort = _activePlanetList.Find(x => x.Planet.Id == planet.Id);
            if (activePlanetSort.Planet != null)
            {
                int newSortingValue = GetActivePlanetSortingValue(planet);
                if (activePlanetSort.SortingValue != newSortingValue) //sorting value changes
                {
                    _activePlanetList.Remove(activePlanetSort);
                    if (newSortingValue > -1) //if sorting value is now -1, then the planet is not active anymore, don't re-add it
                        AddToActivePlanetSorted(planet); //re-add it so it's re-sorted correctly
                    RefreshShownPlanetEntryList(false);
                }
                //do nothing if the sorting value is not changed, possibly because the finished transfer is the lower energy transfer
            }
            else //new active planet
            {
                if (GetActivePlanetSortingValue(planet) == -1) return; //do nothing, this can happen when a planet changes control and its transfers are refreshed
                AddToActivePlanetSorted(planet);
                RefreshShownPlanetEntryList(false);
            }
        }

        public void OnPlanetControlChanged(Planet takenPlanet, Player newOwner, Player previousOwner)
        {
            if (newOwner.IsLocal)
            {
                if (!_planetList.Exists(x => x.Planet.Id == takenPlanet.Id))
                {
                    AddToAllPlanetsSorted(takenPlanet);
                    RefreshShownPlanetEntryList(true);
                }
            }
            else if (previousOwner.IsLocal)
            {
                if (RemovePlanet(_planetList, takenPlanet))
                {
                    RefreshShownPlanetEntryList(true);
                }
                if (RemovePlanet(_activePlanetList, takenPlanet))
                {
                    RefreshShownPlanetEntryList(false);
                }
            }
        }

        private void OnPlanetSelected(Planet selectedPlanet, bool isOwned, SpaceEnvironment environment)
        {
            if (isOwned)
            {
                _selectedPlanet = selectedPlanet.Id;
                RefreshShownPlanetEntryList(_isShowingAll);
            }
        }

        private void OnPlanetDeselected()
        {
            if (_selectedPlanet != null)
            {
                _selectedPlanet = "";
                RefreshShownPlanetEntryList(_isShowingAll);
            }
        }
        #endregion

        #region Private Methods - Planet Entries Management

        private void RefreshShownPlanetEntryList(bool refreshAllPlanetTab)
        {
            if (_isShowingAll != refreshAllPlanetTab) return;

            List<PlanetAndSortingValue> refreshedPlanetList = _isShowingAll ? _planetList : _activePlanetList;

            _firstShownIndex = Mathf.FloorToInt(_contentRect.anchoredPosition.y / _entryHeight);
            var firstUpdatedIndex = _firstShownIndex - _entryOffset;
            if (firstUpdatedIndex < 0) firstUpdatedIndex = 0;
            var lastUpdatedIndex = _firstShownIndex + _shownEntries + _entryOffset;
            if (lastUpdatedIndex > refreshedPlanetList.Count) lastUpdatedIndex = refreshedPlanetList.Count;

            _topFillerElement.preferredHeight = firstUpdatedIndex * 94;
            _bottomFillerElement.preferredHeight = (refreshedPlanetList.Count - lastUpdatedIndex) * 94;

            int i = 0;
            for (i = firstUpdatedIndex; i < lastUpdatedIndex; i++)
            {
                Planet planet = refreshedPlanetList[i].Planet;
                int entryIndex = i - firstUpdatedIndex;
                if (entryIndex >= _planetIndexEntries.Count)
                {
                    PlanetIndexEntryRoot newEntry = Instantiate(_entryPrefab, _planetIndexEntryRoot);
                    newEntry.Init(_eventManager, _planetIcons);
                    newEntry.gameObject.SetActive(false);
                    _planetIndexEntries.Add(newEntry);
                    _bottomFillerElement.transform.SetAsLastSibling();
                }

                if (!_planetIndexEntries[entryIndex].gameObject.activeInHierarchy || planet.Id != _planetIndexEntries[entryIndex].Planet.Id) //updating info and refreshing button behavior only needed if the entry changes
                {
                    if (!_planetIndexEntries[entryIndex].gameObject.activeInHierarchy)
                        _planetIndexEntries[entryIndex].gameObject.SetActive(true);
                    _planetIndexEntries[entryIndex].Show(planet, planet.Id == _selectedPlanet);
                }
            }
            for (int j = i - firstUpdatedIndex; j < _planetIndexEntries.Count; j++)
            {
                _planetIndexEntries[j].gameObject.SetActive(false);
            }
        }

        private void AddToActivePlanetSorted(Planet newPlanet)
        {
            int i = -1;
            var newPlanetSort = new PlanetAndSortingValue() { Planet = newPlanet, SortingValue = GetActivePlanetSortingValue(newPlanet) };
            Debug.Log("adding active planet: " + newPlanet.Id + " with sorting value: " + newPlanetSort.SortingValue);
            while (i < _activePlanetList.Count - 1 && newPlanetSort.SortingValue < _activePlanetList[i + 1].SortingValue)
            {
                i++;
            }
            _activePlanetList.Insert(i + 1, newPlanetSort);
        }

        private void AddToAllPlanetsSorted(Planet newPlanet)
        {
            int i = -1;
            var newPlanetSort = new PlanetAndSortingValue() { Planet = newPlanet, SortingValue = newPlanet.Level };
            while (i < _planetList.Count-1 && newPlanetSort.SortingValue < _planetList[i + 1].SortingValue)
            {
                i++;
            }
            _planetList.Insert(i + 1, newPlanetSort);
        }

        private bool RemovePlanet(List<PlanetAndSortingValue> list, Planet planet)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Planet == planet)
                {
                    list.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        private int GetActivePlanetSortingValue(Planet planet)
        {
            if (planet.GetBiggestHostileInbound() > -1)
            {
                return planet.GetBiggestHostileInbound() + 2000000;
            }
            else if (planet.GetBiggestFriendlyInbound() > -1)
            {
                return planet.GetBiggestFriendlyInbound() + 1000000;
            }
            else return planet.GetBiggestExport();
        }
        #endregion
    }
}
