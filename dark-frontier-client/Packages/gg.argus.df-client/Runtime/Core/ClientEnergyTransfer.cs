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

using ArgusLabs.DF.Core.Space;
using UnityEngine;

namespace ArgusLabs.DF.Core
{
    public class ClientEnergyTransfer
    {
        public ulong Id { get; set; }
        public Planet Source { get; set; }
        public Planet Destination { get; set; }
        public float Progress { get; set; }
        public float TravelDuration { get; set; }
        public int EnergyOnEmbark { get; set; }
        public Player ShipOwner { get; set; }
        public float DurationRemaining => TravelDuration * (1f - Progress);
        public Color LineColor => ShipOwner.IsLocal ? ColorPalette.LocalPlayer : ColorPalette.Enemy;
        public Vector2Int TransferDirectionVector => Source is null ? new Vector2Int(-1,1) : Destination.Position - Source.Position;
    }
}