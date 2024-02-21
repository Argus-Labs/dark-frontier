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
using UnityEngine.UI;
using UnityEngine.Video;

namespace ArgusLabs.DF.UI
{
    public class TutorialVideoController : MonoBehaviour
    {
        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private GameObject _loadingBlocker;
        [SerializeField] private Button _replayButton;

        private void OnLoopPointReached(VideoPlayer source)
        {
            _replayButton.gameObject.SetActive(true);
        }

        private void ReplayVideo()
        {
            _videoPlayer.Play();
            _replayButton.gameObject.SetActive(false);
        }

        public void Prepare()
        {
            gameObject.SetActive(true);
            _loadingBlocker.SetActive(true);
            _replayButton.gameObject.SetActive(false);
            _videoPlayer.prepareCompleted += OnPrepareCompleted;
            _videoPlayer.loopPointReached += OnLoopPointReached;
            _videoPlayer.Prepare();
            _replayButton.onClick.AddListener(ReplayVideo);
        }

        private void OnDisable()
        {
            _videoPlayer.prepareCompleted -= OnPrepareCompleted;
            _videoPlayer.loopPointReached -= OnLoopPointReached;
            _replayButton.onClick.RemoveAllListeners();
        }

        private void OnPrepareCompleted(VideoPlayer source)
        {
            _loadingBlocker.SetActive(false);
            _videoPlayer.Play();
        }

        public void SetUrl(string url)
        {
            _videoPlayer.url = url;
        }
    }
}
