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
    public class NebulaBackground : MonoBehaviour
    {
        [SerializeField] private float _MinSize = 1.2f;
        [SerializeField] private float _MaxOrthoSize = 20f;
        [SerializeField] private AnimationCurve _ScaleCurve;

        private Camera _mainCamera;
        private float _camOrthoSize = 0f;
        private float _camAspectRatio = 0f;
        private float _scale = 1f;

        private Material _backgroundMat;
        
        private void Awake()
        {
            _mainCamera = Camera.main;
            _backgroundMat = GetComponent<MeshRenderer>().material;
            _backgroundMat.SetFloat("_CamOrthoSize", _camOrthoSize);
        }

        private void LateUpdate()
        {
            if (!Mathf.Approximately(_camOrthoSize, _mainCamera.orthographicSize) || _camAspectRatio != _mainCamera.aspect)
            {
                
                _camOrthoSize = _mainCamera.orthographicSize;
                _backgroundMat.SetFloat("_CamOrthoSize", _camOrthoSize);
                _camAspectRatio = _mainCamera.aspect;

                float currentZoomProgress = _ScaleCurve.Evaluate(Mathf.Clamp01(_camOrthoSize / _MaxOrthoSize));
                _scale = Mathf.Lerp(_MinSize, 1f, currentZoomProgress);
                float size = 1f;
                
                if (_camAspectRatio > 1f)
                {
                    size = _camOrthoSize * 2f * _camAspectRatio * _scale;
                }
                else
                {
                    size = _camOrthoSize * 2f * _scale;
                }

                Vector3 newScale = new Vector3(size, size, 1f);
                transform.localScale = newScale;
            }
        }
    }
}
