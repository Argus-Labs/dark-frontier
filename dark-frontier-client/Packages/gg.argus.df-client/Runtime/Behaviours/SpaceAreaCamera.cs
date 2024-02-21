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
    public class SpaceAreaCamera : MonoBehaviour
    {
        [SerializeField] private Camera _spaceAreaCamera;
        private Camera _mainCamera;


        void Start()
        {
            _mainCamera = Camera.main;
        }


        private void LateUpdate()
        {
            if (_mainCamera.aspect > 1f)
            {
                if (!Mathf.Approximately(_spaceAreaCamera.orthographicSize, _mainCamera.orthographicSize * _mainCamera.aspect))
                {
                    _spaceAreaCamera.orthographicSize = _mainCamera.orthographicSize * _mainCamera.aspect;
                }
            }
            else
            {
                if (!Mathf.Approximately(_spaceAreaCamera.orthographicSize, _mainCamera.orthographicSize))
                {
                    _spaceAreaCamera.orthographicSize = _mainCamera.orthographicSize;
                }
            }

            // if (!Mathf.Approximately(_spaceAreaCamera.orthographicSize, _mainCamera.orthographicSize) || _aspectRatio != _mainCamera.aspect)
            // {
            //     _aspectRatio = _mainCamera.aspect;

            //     if(_aspectRatio > 1)
            //     {
            //         _spaceAreaCamera.orthographicSize = _mainCamera.orthographicSize * _aspectRatio;
            //     }
            //     else
            //     {
            //         _spaceAreaCamera.orthographicSize = _mainCamera.orthographicSize;
            //     }
            // }
        }
    }
}
