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

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI
{
    public class UILoadingGraphic : MonoBehaviour
    {
        [SerializeField] private Image[] _image_ActiveBars;

        [SerializeField] private float _fillSpeed = 1f;

        private int _currentIndex;


        private void OnEnable()
        {
            ResetBar();
            _currentIndex = 0;

            StartCoroutine(coroutine_AnimateBar());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }


        private IEnumerator coroutine_AnimateBar()
        {
            float fillProgress = 0;
            _image_ActiveBars[_currentIndex].fillAmount = fillProgress;
            _image_ActiveBars[_currentIndex].fillOrigin = (int)Image.OriginHorizontal.Left;
            yield return null;

            while (fillProgress <= 1)
            {
                _image_ActiveBars[_currentIndex].fillAmount = fillProgress;
                fillProgress += Time.deltaTime * _fillSpeed;
                yield return null;
            }

            fillProgress = 1;
            _image_ActiveBars[_currentIndex].fillAmount = fillProgress;
            _image_ActiveBars[_currentIndex].fillOrigin = (int)Image.OriginHorizontal.Right;
            yield return null;

            while (fillProgress >= 0)
            {
                _image_ActiveBars[_currentIndex].fillAmount = fillProgress;
                fillProgress -= Time.deltaTime * _fillSpeed;
                yield return null;
            }

            fillProgress = 0;
            _image_ActiveBars[_currentIndex].fillAmount = fillProgress;
            _image_ActiveBars[_currentIndex].fillOrigin = (int)Image.OriginHorizontal.Left;
            yield return null;

            _currentIndex++;
            if (_currentIndex >= _image_ActiveBars.Length)
            {
                _currentIndex = 0;
            }
            StartCoroutine(coroutine_AnimateBar());
        }

        private void ResetBar()
        {
            for (int i = 0; i < _image_ActiveBars.Length; i++)
            {
                _image_ActiveBars[i].fillAmount = 0;
                _image_ActiveBars[i].fillOrigin = (int)Image.OriginHorizontal.Left;
            }
        }
    }
}
