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
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

// ReSharper disable InconsistentNaming - We are using the official C# naming conventions.
// It's just confused about the serialization aspect because it can't tell the difference
// between public and private member variables and we have a different rule for private.

namespace ArgusLabs.DF.Core.Configs
{
    // At least as of now, this is an officially weird spot for this. TODO: Think and fix.
    public enum SpaceEnvironment
    {
        BlankSpace = -1,
        Nebula = 0,
        SafeSpace = 1,
        DeepSpace = 2,
        DarkSpace = 3
    }
    
    [Serializable]
    public sealed class SpaceAreasConfig
    {
        public static string Key => "space";
        
        [SerializeField]
        private SpaceAreaRawConfig[] constants;

        public SpaceAreaConfig[] SpaceAreaConfigs { get; private set; }

        public static bool FromJson(string json, out SpaceAreasConfig spaceAreasConfig)
        {
            bool result = true;
            SpaceAreaRawConfig[] rawConfigs = JsonUtility.FromJson<SpaceAreasConfig>(json).constants;
            
            var configs = new SpaceAreaConfig[rawConfigs.Length];
            
            for (int i = 0; i < rawConfigs.Length; ++i)
            {
                var raw = rawConfigs[i];

                if (!Enum.TryParse<SpaceEnvironment>(raw.Label, true, out var spaceEnvironment))
                    result = false;

                configs[i] = new SpaceAreaConfig(
                    spaceEnvironment,
                    raw.PlanetSpawnThreshold,
                    //"0.03",
                    raw.PlanetLevelThreshold,
                    raw.StatBuffMultiplier,
                    raw.DefenseDebuffMultiplier
                );
            }

            spaceAreasConfig = new SpaceAreasConfig { SpaceAreaConfigs = configs };
            
            return result;
        }
        
        public override string ToString()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"{nameof(SpaceAreaConfigs)}:");
            sb.AppendLine();

            foreach (var area in SpaceAreaConfigs)
                sb.AppendLine(area.ToString());

            return sb.ToString();
        }
    }

    [Serializable]
    public class SpaceAreaConfig
    {
        public static SpaceAreaConfig Dummy { get; } = new SpaceAreaConfig();
        public SpaceEnvironment Environment { get; private set; }
        public float PlanetSpawnThreshold { get; private set; }
        public float[] PlanetLevelThresholds { get; private set; }
        public float StatBuffMultiplier { get; private set; }
        public float DefenseDebuffMultiplier { get; private set; }
        
        public SpaceAreaConfig(
            SpaceEnvironment environment,
            string planetSpawnThreshold,
            string[] planetLevelThresholds,
            string statBuffMultiplier,
            string defenseDebuffMultiplier)
        {
            Environment = environment;
            PlanetSpawnThreshold = float.Parse(planetSpawnThreshold, CultureInfo.InvariantCulture);
            PlanetLevelThresholds = (from o in planetLevelThresholds select float.Parse(o, CultureInfo.InvariantCulture)).ToArray();
            StatBuffMultiplier = float.Parse(statBuffMultiplier, CultureInfo.InvariantCulture);
            DefenseDebuffMultiplier = float.Parse(defenseDebuffMultiplier, CultureInfo.InvariantCulture);
        }

        private SpaceAreaConfig() { /* testing only */ }
        
        public override string ToString()
        {
            return @$"{nameof(Environment)}:{Environment}
{nameof(PlanetSpawnThreshold)}:{PlanetSpawnThreshold}
{nameof(PlanetLevelThresholds)}(s):{string.Join(',', PlanetLevelThresholds)}
{nameof(StatBuffMultiplier)}:{StatBuffMultiplier}
{nameof(DefenseDebuffMultiplier)}:{DefenseDebuffMultiplier}
";
        }
    }

    [Serializable]
    public class SpaceAreaRawConfig
    {
        [SerializeField]
        public string Label;
        [SerializeField]
        public string PlanetSpawnThreshold;
        [SerializeField]
        public string[] PlanetLevelThreshold; // <-- typo (should be plural) in source data.
        [SerializeField]
        public string StatBuffMultiplier;
        [SerializeField]
        public string DefenseDebuffMultiplier;
    }
}