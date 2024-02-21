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
using UnityEngine;
using static Unity.Mathematics.math;

namespace ArgusLabs.DF.Core.Configs
{
    [CreateAssetMenu(menuName = "Create GameplayConfig", fileName = "GameplayConfig", order = 0)]
    [Serializable]
    public class GameplayConfig : ScriptableObject
    {
        [Header("Timing")]
        
        [SerializeField] private float _worldClockResyncPeriod = 60f;
        [SerializeField] private float _planetUpdatePeriod = 2f;
        [SerializeField] private float _planetUpdateMaxBatchSize = 100;
        
        [Header("Lines")]
        
        [SerializeField] private float _transferLineGapToDashRatio = 1f;
        [SerializeField] private Vector2 _transferLineDashScale = new Vector2(15f, 6f);
        
        [SerializeField] private float _transferPreviewLineGapToDashRatio = .382f;
        [SerializeField] private Vector2 _transferPreviewLineDashScale = new Vector2(15f, 6f);
        
        [Header("Rings")]
        
        [SerializeField] private float _energyRingThinStrokeWidth = 0.75f;
        [SerializeField] private float _energyRingThickStrokeWidth = 3.5f;
        [SerializeField] private float _locallyOwnedRangeRingStrokeWidth = 2f;
        [SerializeField] private float _selectedPlanetStrokeWidth = 2f;
        [SerializeField] private int _selectedPlanetMinSegments = 32;
        
        public float PlanetUpdatePeriod => _planetUpdatePeriod;
        public float PlanetUpdateMaxBatchSize => _planetUpdateMaxBatchSize;
        public float WorldClockResyncPeriod => _worldClockResyncPeriod;
        public float TransferLineGapToDashRatio => _transferLineGapToDashRatio;
        public Vector2 TransferLineDashScale => _transferLineDashScale;
        public float TransferPreviewLineGapToDashRatio => _transferPreviewLineGapToDashRatio;
        public Vector2 TransferPreviewLineDashScale => _transferPreviewLineDashScale;
        
        public float EnergyRingThinStrokeWidth => _energyRingThinStrokeWidth;
        public float EnergyRingThickStrokeWidth => _energyRingThickStrokeWidth;
        public float LocallyOwnedRangeRingStrokeWidth => _locallyOwnedRangeRingStrokeWidth;
        public float SelectedPlanetStrokeWidth => _selectedPlanetStrokeWidth;
        public int SelectedPlanetMinSegments => _selectedPlanetMinSegments;

        private void Awake() => OnValidate();

        private void OnValidate()
        {
            _planetUpdatePeriod = max(1f, _planetUpdatePeriod);
            _planetUpdateMaxBatchSize = max(1, _planetUpdateMaxBatchSize);
            _transferLineGapToDashRatio = max(0f, _transferLineGapToDashRatio);
            _transferLineDashScale.x = max(.01f, _transferLineDashScale.x);
            _transferLineDashScale.y = max(.01f, _transferLineDashScale.y);
            
            _transferPreviewLineGapToDashRatio = max(0f, _transferPreviewLineGapToDashRatio);
            _transferPreviewLineDashScale.x = max(.01f, _transferPreviewLineDashScale.x);
            _transferPreviewLineDashScale.y = max(.01f, _transferPreviewLineDashScale.y);
            
            _energyRingThinStrokeWidth = max(.01f, _energyRingThinStrokeWidth);
            _energyRingThickStrokeWidth = max(.01f, _energyRingThickStrokeWidth);
            _locallyOwnedRangeRingStrokeWidth = max(.01f, _locallyOwnedRangeRingStrokeWidth);
            _selectedPlanetStrokeWidth = max(.01f, _selectedPlanetStrokeWidth);
            _selectedPlanetMinSegments = max(0, _selectedPlanetMinSegments);
        }
    }
}