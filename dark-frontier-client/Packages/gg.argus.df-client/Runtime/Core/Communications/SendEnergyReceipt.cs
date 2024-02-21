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
    public struct SendEnergyReceipt
    {
        [SerializeField] private SendEnergyReply result;
        [SerializeField] private string txHash;
        [SerializeField] private string[] errors;
        
        public bool HasErrors => errors?.Length > 0;
        public string TxHash => txHash;
        public string[] Errors => errors;
        public ShipReceipt Ship => result.SentShip;
        public PlanetReceipt Planet => result.Planet;
        public double SourceEnergyRemaining => result.SourceEnergyRemaining;
    }
}