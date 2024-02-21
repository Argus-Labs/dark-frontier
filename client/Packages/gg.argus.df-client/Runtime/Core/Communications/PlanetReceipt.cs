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
    public class PlanetReceipt
    {
        [SerializeField] private int level;
        [SerializeField] private string locationHash;
        [SerializeField] private string ownerPersonaTag;
        [SerializeField] private string energyCurrent;
        [SerializeField] private string energyMax;
        [SerializeField] private string energyRefill;
        [SerializeField] private string defense;
        [SerializeField] private string range;
        [SerializeField] private string speed;
        [SerializeField] private string lastUpdateRefillAge;
        [SerializeField] private string lastUpdateTick;

        public int Level => level;
        public string LocationHash => locationHash;
        public string OwnerPersonaTag => ownerPersonaTag;
        public double EnergyLevel => double.Parse(energyCurrent, CultureInfo.InvariantCulture);
        public double EnergyMax => double.Parse(energyMax, CultureInfo.InvariantCulture);
        public double EnergyRefill => double.Parse(energyRefill, CultureInfo.InvariantCulture);
        public double Defense => double.Parse(defense, CultureInfo.InvariantCulture);
        public double Range => double.Parse(range, CultureInfo.InvariantCulture);
        public double Speed => double.Parse(speed, CultureInfo.InvariantCulture);
        public double LastUpdateRefillAge => double.Parse(lastUpdateRefillAge, CultureInfo.InvariantCulture);
        public ulong WorldTick => ulong.Parse(lastUpdateTick, CultureInfo.InvariantCulture);
    }
}