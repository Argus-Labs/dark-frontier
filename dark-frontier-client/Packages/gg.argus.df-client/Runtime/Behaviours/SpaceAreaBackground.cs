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

using UnityEngine;

namespace ArgusLabs.DF.Behaviours
{
    public class SpaceAreaBackground : MonoBehaviour
    {
        private Camera _mainCamera;
        private float _camOrthoSize = 0f;
        private float _camAspectRatio = 0f;


        private void Start()
        {
            _mainCamera = Camera.main;
        }


        private void LateUpdate()
        {
            if (!Mathf.Approximately(_camOrthoSize, _mainCamera.orthographicSize) || _camAspectRatio != _mainCamera.aspect)
            {
                _camOrthoSize = _mainCamera.orthographicSize;
                _camAspectRatio = _mainCamera.aspect;

                float size = 1f;
                if (_camAspectRatio > 1f)
                {
                    size = _camOrthoSize * 2f * _camAspectRatio;
                }
                else
                {
                    size = _camOrthoSize * 2f;
                }

                Vector3 newScale = new Vector3(size, size, 1f);
                transform.localScale = newScale;
            }
        }
    }
}
