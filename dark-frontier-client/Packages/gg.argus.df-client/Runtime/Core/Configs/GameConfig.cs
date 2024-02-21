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

using Unity.Mathematics;

namespace ArgusLabs.DF.Core.Configs
{
    public sealed class GameConfig
    {
        private readonly WorldConfig _worldConfig;
        private readonly PlanetsConfig _planetsConfig;
        private readonly SpaceAreasConfig _spaceAreasConfig;

        public int RadiusMaxOverride { get; set; } = 1;
        public int PerlinScaleOverride { get; set; } = 1;
        
        public string LocationHashSeedWord => _worldConfig.MiMCSeedWord;
        public string PerlinSeedWord => _worldConfig.PerlinSeedWord;
        public int LocationHashNumRounds => _worldConfig.MiMCNumRounds;
        public int PerlinNumRounds => _worldConfig.PerlinNumRounds;
        public bool2 WillMirror => new bool2(_worldConfig.XMirror != 0, _worldConfig.YMirror != 0);
        public int Scale => (PerlinScaleOverride < 1) ? _worldConfig.Scale : PerlinScaleOverride;
        public long WorldRadius => (RadiusMaxOverride < 1) ? _worldConfig.RadiusMax : RadiusMaxOverride;
        public long[] SpacePerlinThresholds => _worldConfig.SpacePerlinThresholds;
        public string InstanceName => _worldConfig.InstanceName;
        public int InstanceTimer => _worldConfig.InstanceTimer;
        public int TicksPerSecond => _worldConfig.TickRate;
        
        public PlanetConfig[] Planets => _planetsConfig.Planets;
        public SpaceAreaConfig[] SpaceAreas => _spaceAreasConfig.SpaceAreaConfigs;
        

        public GameConfig(WorldConfig worldConfig, PlanetsConfig planetsConfig, SpaceAreasConfig spaceAreasConfig)
        {
            _worldConfig = worldConfig;
            _planetsConfig = planetsConfig;
            _spaceAreasConfig = spaceAreasConfig;
        }
    }
}