using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace MouseConfettiWPF.Scripts
{
    internal static class ColorTools
    {
        /// <summary>
        /// Converts HSV to Color. All values are normalized from 0.0 to 1.0.
        /// </summary>
        public static Color FromHSV(float h, float s, float v)
        {
            h = Clamp01(h);
            s = Clamp01(s);
            v = Clamp01(v);

            float r = 0, g = 0, b = 0;

            if (s == 0) r = g = b = v;
            else
            {
                float sector = h * 6f; // [0,6)
                int i = (int)Math.Floor(sector);
                float f = sector - i;
                float p = v * (1f - s);
                float q = v * (1f - s * f);
                float t = v * (1f - s * (1f - f));

                switch (i % 6)
                {
                    case 0: r = v; g = t; b = p; break;
                    case 1: r = q; g = v; b = p; break;
                    case 2: r = p; g = v; b = t; break;
                    case 3: r = p; g = q; b = v; break;
                    case 4: r = t; g = p; b = v; break;
                    case 5: r = v; g = p; b = q; break;
                }
            }

            return Color.FromRgb(
                (byte)(r * 255f),
                (byte)(g * 255f),
                (byte)(b * 255f)
            );
        }

        /// <summary>
        /// Converts HSV to Color. All values are a byte range from 0 to 255.
        /// </summary>
        public static Color FromHSV(byte h, byte s, byte v)
        {
            return FromHSV(h / 255f, s / 255f, v / 255f);
        }

        private static float Clamp01(float f) => Math.Max(0f, Math.Min(1f, f));
    }
}
