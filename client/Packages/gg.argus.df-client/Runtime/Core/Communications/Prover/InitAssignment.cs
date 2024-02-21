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
    /// This is for generating a proof for tx-claim-home-planet.
    /// </summary>
    [Serializable]
    public sealed class InitAssignment
    {
        public string x;
        public string y;
        public string r;
        public string scale;
        public string x_mirror;
        public string y_mirror;
        public string pub; // LocationHash
        public string perl;

        public InitAssignment(Vector2Int p, string locationHash, int perlinValue, GameConfig config)
        {
            x = p.x.ToString();
            y = p.y.ToString();
            r = config.WorldRadius.ToString();
            scale = config.Scale.ToString();
            x_mirror = (config.WillMirror.x ? 1 : 0).ToString();
            y_mirror = (config.WillMirror.y ? 1 : 0).ToString();
            pub = locationHash;
            perl = perlinValue.ToString();
        }
    }
}