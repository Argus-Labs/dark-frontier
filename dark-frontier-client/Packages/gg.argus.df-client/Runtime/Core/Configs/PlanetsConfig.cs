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
using System.Text;
using UnityEngine;

// ReSharper disable InconsistentNaming - We are using the official C# naming conventions.
// It's just confused about the serialization aspect because it can't tell the difference
// between public and private member variables and we have a different rule for private.

namespace ArgusLabs.DF.Core.Configs
{
    [Serializable]
    public sealed class PlanetsConfig
    {
        [SerializeField]
        private PlanetRawConfig[] constants;
        
        public PlanetConfig[] Planets { get; private set; }
        
        public static string Key => "base_planet_level_stats";

        public static PlanetsConfig FromJson(string json)
        {
            PlanetRawConfig[] rawConfigs = JsonUtility.FromJson<PlanetsConfig>(json).constants;
            
            var configs = new PlanetConfig[rawConfigs.Length];
            
            for (int i = 0; i < rawConfigs.Length; ++i)
            {
                var raw = rawConfigs[i];
                
                configs[i] = new PlanetConfig(
                    raw.Level,
                    raw.EnergyDefault,
                    raw.EnergyMax,
                    raw.EnergyRefill,
                    raw.Range,
                    raw.Speed,
                    raw.Defense
                );
            }

            return new PlanetsConfig { Planets = configs };
        }
        

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{nameof(Planets)}:");
            sb.AppendLine();
            
            foreach (var area in Planets)
                sb.AppendLine(area.ToString());

            return sb.ToString();
        }
    }

    public class PlanetConfig
    {
        public int Level { get; private set; }
        public int EnergyDefault { get; private set; }
        public int EnergyMax { get; private set; }
        public int EnergyRefill { get; private set; }
        public int Range { get; private set; }
        public float Speed { get; private set; }
        public int Defense { get; private set; }

        public PlanetConfig(int level, string energyDefault, string energyMax, string energyRefill, string range, string speed, string defense)
        {
            Level = level;
            EnergyDefault = int.Parse(energyDefault, CultureInfo.InvariantCulture);
            EnergyMax = int.Parse(energyMax, CultureInfo.InvariantCulture);
            EnergyRefill = int.Parse(energyRefill, CultureInfo.InvariantCulture);
            Range = int.Parse(range, CultureInfo.InvariantCulture);
            Speed = float.Parse(speed, CultureInfo.InvariantCulture);
            Defense = int.Parse(defense, CultureInfo.InvariantCulture);
        }
        
        public PlanetConfig(int level, int energyDefault = 0, int energyMax = 0, int energyRefill = 0, int range = 0, float speed = 0, int defense = 0)
        {
            Level = level;
            EnergyDefault = energyDefault;
            EnergyMax = energyMax;
            EnergyRefill = energyRefill;
            Range = range;
            Speed = speed;
            Defense = defense;
        }
        
        public override string ToString()
        {
            return $@"{nameof(Level)}:{Level}
{nameof(EnergyDefault)}:{EnergyDefault}
{nameof(EnergyMax)}:{EnergyMax}
{nameof(EnergyRefill)}:{EnergyRefill}
{nameof(Range)}:{Range}
{nameof(Speed)}:{Speed}
{nameof(Defense)}:{Defense}
";
        }
    }

    [Serializable]
    public class PlanetRawConfig
    {
        public int Level;
        public string EnergyDefault;
        public string EnergyMax;
        public string EnergyRefill;
        public string Range;
        public string Speed;
        public string Defense;
    }
}