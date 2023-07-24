using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace OpaOpaOpasity
{

    public class MathHSL
    {
        /// <summary>
        /// RGBをHSLに変換
        /// h = 0 to 360.0, s,l = 0 to 1.0
        /// 無彩色時のhは360で返す
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static (double h, double s, double l) Rgb2Hsl(byte r, byte g, byte b)
        {
            byte min = Math.Min(b, Math.Min(r, g));
            byte max = Math.Max(b, Math.Max(r, g));

            double total = max + min;
            //輝度L
            double l = total / 2.0 / 255;

            double h;
            double s;
            //無彩色
            if (max == min)
            {
                //未定義
                h = 0.0; s = 0.0;
                //
                //h = 360.0;s = 0.0;
            }
            //有彩色
            else
            {
                double diff = max - min;
                //色相H
                if (max == r)
                {
                    h = (g - b) / diff * 60;
                    if (h < 0) { h += 360.0; }
                }
                else if (max == g)
                {
                    h = (b - r) / diff * 60 + 120;
                }
                else
                {
                    h = (r - g) / diff * 60 + 240;
                }

                //彩度S
                if (0 <= l && l <= 0.5)
                {
                    s = diff / total;
                }
                else
                {
                    s = diff / (510 - total);
                }
            }
            return (h, s, l);
        }



        //色の変換（カラーコード・RGB・HSV・HSL） ／ ツール - Hirota Yano
        //        https://yanohirota.com/color-converter/

        public static (byte r, byte g, byte b) Hsl2Rgb(double h, double s, double l)
        {
            double hue = h;
            if (hue == 360) { hue = 0; }

            double lum = l;
            if (0.5 <= l)
            {
                lum = 1.0 - l;
            }

            double max = 255 * (l + lum * s);
            double min = 255 * (l - lum * s);
            double diff = max - min;
            double rr, gg, bb;
            if (0 <= hue && hue < 60)
            {
                rr = max;
                gg = FFF(hue);
                bb = min;
            }
            else if (60 <= hue && hue < 120)
            {
                rr = FFF(120 - hue);
                gg = max;
                bb = min;
            }
            else if (120 <= hue && hue < 180)
            {
                rr = min;
                gg = max;
                bb = FFF(hue - 120);
            }
            else if (180 <= hue && hue < 240)
            {
                rr = min;
                gg = FFF(240 - hue);
                bb = max;
            }
            else if (240 <= hue && hue < 300)
            {
                rr = FFF(hue - 240);
                gg = min;
                bb = max;
            }
            else if (300 <= hue && hue < 360)
            {
                rr = max;
                gg = min;
                bb = FFF(360 - hue);
            }
            else
            {
                rr = 0;
                gg = 0;
                bb = 0;
            }

            return (
                (byte)Math.Round(rr, MidpointRounding.AwayFromZero),
                (byte)Math.Round(gg, MidpointRounding.AwayFromZero),
                (byte)Math.Round(bb, MidpointRounding.AwayFromZero));

            double FFF(double x)
            {
                return (x / 60 * diff) + min;
            }


        }
    }
}
