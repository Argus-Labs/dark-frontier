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

using System.Collections.Generic;
using ArgusLabs.DF.Core.Space;
using Smonch.CyclopsFramework;
using UnityEngine;

namespace ArgusLabs.DF.Core
{
    public class ExplorerRoutine : CyclopsRoutine
    {
        private readonly Explorer _explorer;
        private readonly SpaceMap _map;
        
        private readonly Dictionary<Vector2Int, Vector2Int> _directions = new()
        {
            [Vector2Int.zero] = Vector2Int.right,
            [Vector2Int.right] = Vector2Int.down,
            [Vector2Int.down] = Vector2Int.left,
            [Vector2Int.left] = Vector2Int.up,
            [Vector2Int.up] = Vector2Int.right
        };
        
        Sector Here => _map[_explorer.Position];
        
        public ExplorerRoutine(Explorer explorer, SpaceMap map)
            : base(period: double.MaxValue, cycles: double.MaxValue)
        {
            _explorer = explorer;
            _map = map;
        }

        protected override void OnEnter()
        {
            _explorer.Reset();
        }

        // Note: This is the first frame of each cycle.
        // Also, this is now using one async system within another and must be handled with care.
        // We originally used the CyclopsRoutine here for more than just driving the explorer with pause and resume capabilities.
        // It was used to control the explorer stepping period and to provide an elegant option for smooth animation using the OnUpdate(t:float) method as well.
        // I consider this to be a bit of a hack, but it meets our needs for now.
        protected override async void OnFirstFrame()
        {
            void Turn() => _explorer.Heading = _directions[_explorer.Heading];
            void MoveForward() => _explorer.Position += _explorer.Heading;
            bool IsValidHere() => _map.IsValidPosition(_explorer.Position);
            bool shouldStepForward = false;
            
            for (int i = 0; i < 4096 && !shouldStepForward; ++i)
            {
                if (IsValidHere() && !Here.WasExplored)
                {
                    var sector = await _map.ExploreAsync(_explorer.Position, Application.exitCancellationToken);

                    if (Application.exitCancellationToken.IsCancellationRequested)
                    {
                        Stop();
                        return;
                    }

                    shouldStepForward = true;
                }
                
                MoveForward();

                if (--_explorer.RemainingStepCount == 0)
                {
                    _explorer.TwiceSideLength += 1;
                    _explorer.RemainingStepCount = _explorer.TwiceSideLength >> 1;
                    Turn();
                }
            }

            // This increments the cycle manually because the period was set to Double.MaxValue to prevent single-threaded async race conditions.
            StepForward();
        }
    }
}