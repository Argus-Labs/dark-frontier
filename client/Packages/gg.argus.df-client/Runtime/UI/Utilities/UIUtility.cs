using System;
using System.Text;
using ArgusLabs.DF.Core.Configs;
using UnityEngine;

namespace ArgusLabs.DF.UI.Utilities
{
    public static class UIUtility
    {
        /// <summary>
        /// make a string for time in the format of "<HH>h <MM>m <SS>s "
        /// </summary>
        public static string TimeSpanToString(TimeSpan timeSpan)
        {
            StringBuilder builder = new StringBuilder();

            if (timeSpan.Hours > 0)
            {
                builder.Append($"{timeSpan.Hours}h ");
            }

            if (timeSpan.Minutes > 0)
            {
                builder.Append($"{timeSpan.Minutes}m ");
            }

            if (timeSpan.Seconds > 0)
            {
                builder.Append($"{timeSpan.Seconds}s ");
            }

            return builder.ToString().Trim();
        }

        /// <summary>
        /// make a string for time in the format of HH:MM:SS
        /// </summary>
        public static string TimeSpanToClockString(TimeSpan timeSpan)
        {
            StringBuilder builder = new StringBuilder();

            if(timeSpan.Days > 0)
            {
                builder.Append($"{timeSpan.Days}:");
                builder.Append(timeSpan.Hours > 9 ? $"{timeSpan.Hours}:" : $"0{timeSpan.Hours}:");
                builder.Append(timeSpan.Minutes > 9 ? $"{timeSpan.Minutes}:" : $"0{timeSpan.Minutes}:");
                builder.Append(timeSpan.Seconds > 9 ? $"{timeSpan.Seconds}" : $"0{timeSpan.Seconds}");
            }
            else
            {
                builder.Append(timeSpan.Hours > 9 ? $"{timeSpan.Hours}:" : $"0{timeSpan.Hours}:");
                builder.Append(timeSpan.Minutes > 9 ? $"{timeSpan.Minutes}:" : $"0{timeSpan.Minutes}:");
                builder.Append(timeSpan.Seconds > 9 ? $"{timeSpan.Seconds}" : $"0{timeSpan.Seconds}");
            }

            return builder.ToString().Trim();
        }

        public static string SpaceAreaString(SpaceEnvironment environment)
        {
            switch (environment)
            {
                case SpaceEnvironment.Nebula: return "nebula";
                case SpaceEnvironment.SafeSpace: return "safe space";
                case SpaceEnvironment.DeepSpace: return "deep space";
            }
            return "";
        }

        public static string TrimLongString(string str, int maxChar)
        {
            if (str.Length > maxChar)
            {
                return $"{str.Substring(0, maxChar - 3)}...";
            }
            else return str;
        }

        // public static Color GetPlayerColor(string personaTag)
        // {
        //     System.Random rand = new System.Random(personaTag.GetHashCode());
        //     int n = rand.Next(199);
        //     float hue = 3f * (n % 100);
        //     hue = hue > 200f ? hue + 60f: hue; //avoid hue range 200-260 because those are blue
        //     float v = (Mathf.FloorToInt(n / 100) == 0 ? 56 : 76) / 100f;
        //     return Color.HSVToRGB(hue/360f, 81f / 100f, v);
        // }

        public static float GetTransferInfoArmRotation(Vector2 transferDirectionVector)
        {
            if (Mathf.Approximately(transferDirectionVector.x, 0))
            {
                return -40 * Mathf.Deg2Rad;
            }
            else if (Mathf.Approximately(transferDirectionVector.y, 0))
            {
                return -Mathf.PI / 2;
            }
            else
            {
                Vector2 dirVectorNormalized = transferDirectionVector.normalized;
                return Mathf.Clamp(Mathf.Atan(dirVectorNormalized.y / dirVectorNormalized.x) - Mathf.PI / 2, -130 * Mathf.Deg2Rad, -40 * Mathf.Deg2Rad);
            }
        }
    }
}
