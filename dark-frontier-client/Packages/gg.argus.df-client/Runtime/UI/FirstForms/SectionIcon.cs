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

namespace ArgusLabs.DF.UI.FirstForms
{
    public class SectionIcon : MonoBehaviour
    {
        [SerializeField] private Image _frame;
        [SerializeField] private Image _sectionIcon;

        [SerializeField] private Color _inactive;
        [SerializeField] private Color _active;
        [SerializeField] private Color _orange;

        public void SetInactive()
        {
            _frame.color = _active;
            _sectionIcon.color = _inactive;
        }

        public void SetActive()
        {
            _frame.color = _orange;
            _sectionIcon.color = _active;
        }

        public void SetCompleted()
        {
            _frame.color = _orange;
            _sectionIcon.color = _orange;
        }
    }
}
