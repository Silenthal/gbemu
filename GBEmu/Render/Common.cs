using System;

namespace GBEmu.Render
{
    public class Common
    {
        public static uint MASK_2 = 0x0000FF00;
        public static uint MASK_13 = 0x00FF00FF;
        public static uint MASK_RGB = 0x00FFFFFF;
        public static uint MASK_ALPHA = 0xFF000000;

        public static uint Ymask = 0x00FF0000;
        public static uint Umask = 0x0000FF00;
        public static uint Vmask = 0x000000FF;
        public static uint trY = 0x00300000;
        public static uint trU = 0x00000700;
        public static uint trV = 0x00000006;

        public static uint[] RGBtoYUV = new uint[16777216];

        private uint YUV1, YUV2;

        static Common()
        {
            /* Initalize RGB to YUV lookup table */
            uint r, g, b, y, u, v;
            for (uint c = 0; c < 16777215; c++)
            {
                r = (c & 0xFF0000) >> 16;
                g = (c & 0x00FF00) >> 8;
                b = c & 0x0000FF;
                y = (uint)(0.299 * r + 0.587 * g + 0.114 * b);
                u = (uint)(-0.169 * r - 0.331 * g + 0.5 * b) + 128;
                v = (uint)(0.5 * r - 0.419 * g - 0.081 * b) + 128;
                RGBtoYUV[c] = (y << 16) + (u << 8) + v;
            }
        }

        public static uint rgb_to_yuv(uint c)
        {
            // Mask against MASK_RGB to discard the alpha channel
            return RGBtoYUV[MASK_RGB & c];
        }

        /* Test if there is difference in color */

        public static bool yuv_diff(uint yuv1, uint yuv2)
        {
            return ((Math.Abs((yuv1 & Ymask) - (yuv2 & Ymask)) > trY) ||
                    (Math.Abs((yuv1 & Umask) - (yuv2 & Umask)) > trU) ||
                    (Math.Abs((yuv1 & Vmask) - (yuv2 & Vmask)) > trV));
        }

        public static bool Diff(uint c1, uint c2)
        {
            return yuv_diff(rgb_to_yuv(c1), rgb_to_yuv(c2));
        }

        /* Interpolate functions */

        public static uint Interpolate_2(uint c1, int w1, uint c2, int w2, int s)
        {
            if (c1 == c2)
            {
                return c1;
            }
            return
                (uint)
                ((((((c1 & MASK_ALPHA) >> 24) * w1 + ((c2 & MASK_ALPHA) >> 24) * w2) << (24 - s)) & MASK_ALPHA) +
                ((((c1 & MASK_2) * w1 + (c2 & MASK_2) * w2) >> s) & MASK_2) +
                ((((c1 & MASK_13) * w1 + (c2 & MASK_13) * w2) >> s) & MASK_13));
        }

        public static uint Interpolate_3(uint c1, int w1, uint c2, int w2, uint c3, int w3, int s)
        {
            return
                (uint)
                ((((((c1 & MASK_ALPHA) >> 24) * w1 + ((c2 & MASK_ALPHA) >> 24) * w2 + ((c3 & MASK_ALPHA) >> 24) * w3) << (24 - s)) & MASK_ALPHA) +
                ((((c1 & MASK_2) * w1 + (c2 & MASK_2) * w2 + (c3 & MASK_2) * w3) >> s) & MASK_2) +
                ((((c1 & MASK_13) * w1 + (c2 & MASK_13) * w2 + (c3 & MASK_13) * w3) >> s) & MASK_13));
        }

        public static unsafe void Interp1(uint* pc, uint c1, uint c2)
        {
            //*pc = (c1*3+c2) >> 2;
            *pc = Interpolate_2(c1, 3, c2, 1, 2);
        }

        public static unsafe void Interp2(uint* pc, uint c1, uint c2, uint c3)
        {
            //*pc = (c1*2+c2+c3) >> 2;
            *pc = Interpolate_3(c1, 2, c2, 1, c3, 1, 2);
        }

        public static unsafe void Interp3(uint* pc, uint c1, uint c2)
        {
            //*pc = (c1*7+c2)/8;
            *pc = Interpolate_2(c1, 7, c2, 1, 3);
        }

        public static unsafe void Interp4(uint* pc, uint c1, uint c2, uint c3)
        {
            //*pc = (c1*2+(c2+c3)*7)/16;
            *pc = Interpolate_3(c1, 2, c2, 7, c3, 7, 4);
        }

        public static unsafe void Interp5(uint* pc, uint c1, uint c2)
        {
            //*pc = (c1+c2) >> 1;
            *pc = Interpolate_2(c1, 1, c2, 1, 1);
        }

        public static unsafe void Interp6(uint* pc, uint c1, uint c2, uint c3)
        {
            //*pc = (c1*5+c2*2+c3)/8;
            *pc = Interpolate_3(c1, 5, c2, 2, c3, 1, 3);
        }

        public static unsafe void Interp7(uint* pc, uint c1, uint c2, uint c3)
        {
            //*pc = (c1*6+c2+c3)/8;
            *pc = Interpolate_3(c1, 6, c2, 1, c3, 1, 3);
        }

        public static unsafe void Interp8(uint* pc, uint c1, uint c2)
        {
            //*pc = (c1*5+c2*3)/8;
            *pc = Interpolate_2(c1, 5, c2, 3, 3);
        }

        public static unsafe void Interp9(uint* pc, uint c1, uint c2, uint c3)
        {
            //*pc = (c1*2+(c2+c3)*3)/8;
            *pc = Interpolate_3(c1, 2, c2, 3, c3, 3, 3);
        }

        public static unsafe void Interp10(uint* pc, uint c1, uint c2, uint c3)
        {
            //*pc = (c1*14+c2+c3)/16;
            *pc = Interpolate_3(c1, 14, c2, 1, c3, 1, 4);
        }
    }
}