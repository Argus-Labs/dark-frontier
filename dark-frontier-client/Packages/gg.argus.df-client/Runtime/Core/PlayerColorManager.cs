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
using Smonch.CyclopsFramework.Extensions;
using UnityEngine;

namespace ArgusLabs.DF.Core
{
    public class PlayerColorManager
    {
        private readonly Queue<Color> _colors = new();
        private readonly HashSet<Player> _players = new();

        public PlayerColorManager(in int maxUniqueColors = 12)
        {
            System.Random rng = new System.Random(1337);
            
            // Avoid blueish colors. (200-260)
            const float gapStart = 200f;
            const float gapEnd = 260f;
            float gap = gapEnd - gapStart;
            float step = (360f - gap) / maxUniqueColors;

            var palette = new Color[maxUniqueColors];
            int index = 0;
            
            for (float h = gapEnd; h < 360 + gapStart; h += step)
            {
                var c = palette[index++] = Color.HSVToRGB((h / 360f) % 1f, .81f, 1f).WithAlpha(1f);
                Debug.Log("Palette Color: " + c);
            }
            
            for (int i = 0; i < maxUniqueColors - 2; ++i)
            {
                int j = rng.Next(i + 1, maxUniqueColors);
                (palette[i], palette[j]) = (palette[j], palette[i]);
            }
            
            foreach (Color c in palette)
            {
                Debug.Log("Shuffled Palette Color: " + c);
                _colors.Enqueue(c);
            }
            
            // Using a base of .635f and stepping .05f will avoid a collision with the .81f saturation of the base palette colors.
            for (float s = .635f; s <= 1f; s += .05f)
            {
                for (float v = .6f; v <= 1f; v += .05f)
                {
                    for (int i = 0; i < palette.Length; ++i)
                    {
                        Color.RGBToHSV(palette[i], out float h, out float _, out float _);
                        _colors.Enqueue(Color.HSVToRGB(h, s, v).WithAlpha(1f));
                    }
                }
            }
            
            // Use this to dump the colors to a png for debugging.
            // var a = _colors.ToArray();
            // var png = new Texture2D(a.Length, 1);
            // png.SetPixels(a);
            // png.Apply();
            // System.IO.File.WriteAllBytes(@"c:\builds\colors.png", png.EncodeToPNG());
        }
        
        public bool TryAssignColor(Player player)
        {
            if (!_players.Add(player))
                return false;
            
            var c = _colors.Dequeue();
            player.Color = c;
            _colors.Enqueue(c);
            
            Debug.Log($"Assigning {player.Color} to {player.PersonaTag}");
            
            return true;
        }
    }
}