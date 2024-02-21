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
using UnityEngine;

namespace ArgusLabs.DF.Core.Communications
{
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ShipReceipt
    {
        [SerializeField] private ulong id;
        [SerializeField] private string ownerPersonaTag;
        [SerializeField] private string locationHashFrom;
        [SerializeField] private string locationHashTo;
        [SerializeField] private ulong tickStart;
        [SerializeField] private ulong tickArrive;
        [SerializeField] private double energyOnEmbark;

        public ulong Id => id;
        public string OwnerPersonaTag => ownerPersonaTag;
        public string LocationHashFrom => locationHashFrom;
        public string LocationHashTo => locationHashTo;

        public ulong TickStart
        {
            get
            {
                if (tickArrive < tickStart)
                    Debug.LogError("TickArrive should never be less than TickStart.");
                return tickStart;
            }
        }
        
        public ulong TickArrive
        {
            get
            {
                if (tickArrive < tickStart)
                    Debug.LogError("TickArrive should never be less than TickStart.");
                return tickArrive;
            }
        }

        public double DurationInTicks => Math.Max(0, tickArrive - tickStart);
        
        public double EnergyOnEmbark => energyOnEmbark; // Some kind of number.
    }
}