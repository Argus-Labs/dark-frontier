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
using UnityEngine;

namespace ArgusLabs.DF.Core
{
    // As of 11/1/2023 we aren't using this. I'd remove it if I thought we wouldn't use it again going forward,
    // but we have other things that we could be instancing and we just might do that.
    // It can be used to render instanced batches of whatever you'd like provided you have a prefab with exactly 1 mesh and 1 material.
    public class RenderBatcher
    {
        // This is Unity's current limit.
        private const int InstanceLimitPerBatch = 1023;
        
        private RenderParams _renderParams;
        private readonly Mesh _mesh;
        private readonly List<Matrix4x4> _matrices = new();
        
        public RenderBatcher(Mesh mesh, RenderParams renderParams)
        {
            _mesh = mesh;
            _renderParams = renderParams;
        }

        public void Add(Matrix4x4 objectToWorldMatrix) => _matrices.Add(objectToWorldMatrix);
        public void Clear() => _matrices.Clear();

        public void Render(Bounds worldBounds)
        {
            int instanceCount = _matrices.Count;
            int instanceOffset = 0;
            int remainingInstanceCount = instanceCount;

            _renderParams.worldBounds = worldBounds;
            
            while (remainingInstanceCount > 0)
            {
                int batchInstanceCount = Mathf.Min(InstanceLimitPerBatch, instanceCount);
                
                Graphics.RenderMeshInstanced(_renderParams, _mesh,
                    0, _matrices, batchInstanceCount, instanceOffset);
                
                remainingInstanceCount -= batchInstanceCount;
                instanceOffset += batchInstanceCount;
            }
        }
    }
}