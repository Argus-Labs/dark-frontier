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
using ArgusLabs.DF.Core.Configs;
using UnityEngine;

namespace ArgusLabs.DF.Core.Communications.Prover
{
    /// <summary>
    /// This is for generating a proof for tx-send-energy.
    /// </summary>
    [Serializable]
    public sealed class MoveAssignment
    {
        public string x1;
        public string y1;
        public string x2;
        public string y2;
        public string r;
        public string distmax; // MaxDistance from the client
        public string scale;
        public string x_mirror;
        public string y_mirror;
        public string pub1; // LocationHashFrom
        public string pub2; // LocationHashTo
        public string perl2; // PerlinTo

        public MoveAssignment(Vector2Int positionFrom, Vector2Int positionTo, string locationHashFrom, string locationHashTo, int perlinTo, GameConfig config)
        {
            x1 = positionFrom.x.ToString();
            y1 = positionFrom.y.ToString();
            x2 = positionTo.x.ToString();
            y2 = positionTo.y.ToString();
            r = config.WorldRadius.ToString();
            distmax = Mathf.CeilToInt((positionTo - positionFrom).magnitude).ToString(); // TODO: Make sure this matches what we send to the backend!
            scale = config.Scale.ToString();
            x_mirror = (config.WillMirror.x ? 1 : 0).ToString();
            y_mirror = (config.WillMirror.y ? 1 : 0).ToString();
            pub1 = locationHashFrom;
            pub2 = locationHashTo;
            perl2 = perlinTo.ToString();
        }
    }
}