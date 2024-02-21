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
using System.Text;
using UnityEngine;

// ReSharper disable InconsistentNaming - We are using the official C# naming conventions.
// It's just confused about the serialization aspect because it can't tell the difference
// between public and private member variables and we have a different rule for private.

// Note: Members are public for serialization purposes only.
// That said, public members are NOT directly exposed to the game.
// Instead, this class is consumed by GameConfig and proper access restrictions are in place.

namespace ArgusLabs.DF.Core.Configs
{
    [Serializable]
    public sealed class WorldConfig
    {
        [Serializable]
        private sealed class Wrapper
        {
            public WorldConfig constants;
        }
        
        public string MiMCSeedWord; // <-- LocationHashSeedWord
        public string PerlinSeedWord;
        public int MiMCNumRounds; // <-- LocationHashNumRounds
        public int PerlinNumRounds;
        public int XMirror;
        public int YMirror;
        public int Scale;
        public int PlayerCount;
        public int PlayerDensityThreshold;
        public int RadiusCurrent;
        public int RadiusMax;
        public int RadiusGrowth;
        public long[] SpacePerlinThresholds;
        public int PerlinThreshold1;
        public int PerlinThreshold2;
        public int PlayerSpawnMin1;
        public int PlayerSpawnMax1;
        public int PlayerSpawnMin2;
        public int PlayerSpawnMax2;
        public int PlayerSpawnMin3;
        public int PlayerSpawnMax3;
        public string InstanceName;
        public int InstanceTimer;
        public int TickRate;
        
        public static string Key => "world";

        public static WorldConfig FromJson(string json)
            => JsonUtility.FromJson<Wrapper>(json).constants;
        
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{nameof(MiMCSeedWord)}: {MiMCSeedWord}");
            sb.AppendLine($"{nameof(PerlinSeedWord)}: {PerlinSeedWord}");
            sb.AppendLine($"{nameof(MiMCNumRounds)}: {MiMCNumRounds}");
            sb.AppendLine($"{nameof(PerlinNumRounds)}: {PerlinNumRounds}");
            sb.AppendLine($"{nameof(XMirror)}: {XMirror}");
            sb.AppendLine($"{nameof(YMirror)}: {YMirror}");
            sb.AppendLine($"{nameof(Scale)}: {Scale}");
            sb.AppendLine($"{nameof(PlayerCount)}: {PlayerCount}");
            sb.AppendLine($"{nameof(PlayerDensityThreshold)}: {PlayerDensityThreshold}");
            sb.AppendLine($"{nameof(RadiusCurrent)}: {RadiusCurrent}");
            sb.AppendLine($"{nameof(RadiusMax)}: {RadiusMax}");
            sb.AppendLine($"{nameof(RadiusGrowth)}: {RadiusGrowth}");
            sb.AppendLine($"{nameof(SpacePerlinThresholds)}: {SpacePerlinThresholds[0]}, {SpacePerlinThresholds[1]}");
            sb.AppendLine($"{nameof(PerlinThreshold1)}: {PerlinThreshold1}");
            sb.AppendLine($"{nameof(PerlinThreshold2)}: {PerlinThreshold2}");
            sb.AppendLine($"{nameof(PlayerSpawnMin1)}: {PlayerSpawnMin1}");
            sb.AppendLine($"{nameof(PlayerSpawnMax1)}: {PlayerSpawnMax1}");
            sb.AppendLine($"{nameof(PlayerSpawnMin2)}: {PlayerSpawnMin2}");
            sb.AppendLine($"{nameof(PlayerSpawnMax2)}: {PlayerSpawnMax2}");
            sb.AppendLine($"{nameof(PlayerSpawnMin3)}: {PlayerSpawnMin3}");
            sb.AppendLine($"{nameof(PlayerSpawnMax3)}: {PlayerSpawnMax3}");
            sb.AppendLine($"{nameof(InstanceName)}: {InstanceName}");
            sb.AppendLine($"{nameof(InstanceTimer)}: {InstanceTimer}");
            sb.AppendLine($"{nameof(TickRate)}: {TickRate}");

            return sb.ToString();
        }
    }
}