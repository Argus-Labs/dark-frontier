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

namespace ArgusLabs.DF.Core
{
    public static class ColorPalette
    {
        private static readonly Color PaperWhite = new Color(245f / 255f, 245f / 255f, 245f/255f);
        public static readonly Color LocalEnergyLine = PaperWhite;
        public static readonly Color Explorer = PaperWhite;
        public static readonly Color LocalRangeRing = PaperWhite;
        public static readonly Color SelectionRing = PaperWhite;
        public static readonly Color LocalPlayer = PaperWhite;
        public static readonly Color Enemy = new Color(244f / 255f, 0f, 0f);
        public static readonly Color UnclaimedRing = new Color(0.533f, 0.533f, 0.533f);
        public static readonly Color UnclaimedGrey = new Color(84f / 255f, 73f / 255f, 80f / 255f);
    }
}
