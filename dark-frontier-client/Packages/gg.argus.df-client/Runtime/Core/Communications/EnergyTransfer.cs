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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using UnityEngine;

namespace ArgusLabs.DF.Core.Communications
{
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class EnergyTransfer
    {
        [SerializeField] private ulong transferId;
        [SerializeField] private string planetToHash;
        [SerializeField] private string planetFromHash;
        [SerializeField] private int percentCompletion;
        [SerializeField] private string energyOnEmbark;
        [SerializeField] private string ownerPersonaTag;
        [SerializeField] private int travelTimeInSeconds;
        public ulong TransferId => transferId;
        public string SourceHash => planetFromHash;
        public string DestinationHash => planetToHash;
        public float Progress => Mathf.Clamp01(percentCompletion * .01f);
        public float EnergyOnEmbark => float.Parse(energyOnEmbark, CultureInfo.InvariantCulture);
        public string OwnerPersonaTag => ownerPersonaTag;
        public int TravelTimeInSeconds => travelTimeInSeconds;
    }
}