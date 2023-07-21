using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace OpaOpaOpasity
{

    /// <summary>
    /// h = 0 to 360.0, s,l = 0 to 1.0
    /// </summary>
    public class MathHSL
    {
        public (double h, double s, double l) Rgb2Hsl(byte r, byte g, byte b)
        {
            var min = Math.Min(b, Math.Min(r, g));
            var max = Math.Max(b, Math.Max(r, g));
            var diff = max - min;
            double h;
            if (max == r)
            {
                h = (g - b) / diff * 60;
            }
            else if (max == g)
            {
                h = (b - r) / diff * 60 + 120;
            }
            else
            {
                h = (r - g) / diff * 60 + 240;
            }

            double l = (max + min) / 2.0 / 255;

            double s;
            if (0 <= l && l <= 0.5)
            {
                s = diff / (max + min);
            }
            else
            {
                s = diff / (510 - (max + min));
            }
            return (h, s, l);
        }

        public (byte r, byte g, byte b) Hsl2Rgb(double h, double s, double l)
        {

        }
    }
}
