namespace GBEmu.Render
{
    public class Hq2x
    {
        private static unsafe void hq2x_32_rb(uint* sp, uint srb, uint* dp, uint drb, int Xres, int Yres)
        {
            int i, j, k;
            int prevline, nextline;
            uint[] w = new uint[10];
            int dpL = (int)(drb >> 2);
            int spL = (int)(srb >> 2);
            byte* sRowP = (byte*)sp;
            byte* dRowP = (byte*)dp;
            uint yuv1, yuv2;

            //   +----+----+----+
            //   |    |    |    |
            //   | w1 | w2 | w3 |
            //   +----+----+----+
            //   |    |    |    |
            //   | w4 | w5 | w6 |
            //   +----+----+----+
            //   |    |    |    |
            //   | w7 | w8 | w9 |
            //   +----+----+----+

            for (j = 0; j < Yres; j++)
            {
                if (j > 0)
                    prevline = -spL;
                else
                    prevline = 0;
                if (j < Yres - 1)
                    nextline = spL;
                else
                    nextline = 0;

                for (i = 0; i < Xres; i++)
                {
                    w[2] = *(sp + prevline);
                    w[5] = *sp;
                    w[8] = *(sp + nextline);

                    if (i > 0)
                    {
                        w[1] = *(sp + prevline - 1);
                        w[4] = *(sp - 1);
                        w[7] = *(sp + nextline - 1);
                    }
                    else
                    {
                        w[1] = w[2];
                        w[4] = w[5];
                        w[7] = w[8];
                    }

                    if (i < Xres - 1)
                    {
                        w[3] = *(sp + prevline + 1);
                        w[6] = *(sp + 1);
                        w[9] = *(sp + nextline + 1);
                    }
                    else
                    {
                        w[3] = w[2];
                        w[6] = w[5];
                        w[9] = w[8];
                    }

                    int pattern = 0;
                    int flag = 1;

                    yuv1 = Common.rgb_to_yuv(w[5]);

                    for (k = 1; k <= 9; k++)
                    {
                        if (k == 5)
                            continue;

                        if (w[k] != w[5])
                        {
                            yuv2 = Common.rgb_to_yuv(w[k]);
                            if (Common.yuv_diff(yuv1, yuv2))
                                pattern |= flag;
                        }
                        flag <<= 1;
                    }

                    switch (pattern)
                    {
                        case 0:
                        case 1:
                        case 4:
                        case 32:
                        case 128:
                        case 5:
                        case 132:
                        case 160:
                        case 33:
                        case 129:
                        case 36:
                        case 133:
                        case 164:
                        case 161:
                        case 37:
                        case 165:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 2:
                        case 34:
                        case 130:
                        case 162:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 16:
                        case 17:
                        case 48:
                        case 49:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 64:
                        case 65:
                        case 68:
                        case 69:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 8:
                        case 12:
                        case 136:
                        case 140:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 3:
                        case 35:
                        case 131:
                        case 163:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 6:
                        case 38:
                        case 134:
                        case 166:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                Common.Interp1(dp + 1, w[5], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 20:
                        case 21:
                        case 52:
                        case 53:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 144:
                        case 145:
                        case 176:
                        case 177:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 192:
                        case 193:
                        case 196:
                        case 197:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 96:
                        case 97:
                        case 100:
                        case 101:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 40:
                        case 44:
                        case 168:
                        case 172:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 9:
                        case 13:
                        case 137:
                        case 141:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 18:
                        case 50:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[3]);
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 80:
                        case 81:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 72:
                        case 76:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[7]);
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 10:
                        case 138:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    Common.Interp1(dp, w[5], w[1]);
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 66:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 24:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 7:
                        case 39:
                        case 135:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                Common.Interp1(dp + 1, w[5], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 148:
                        case 149:
                        case 180:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 224:
                        case 228:
                        case 225:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 41:
                        case 169:
                        case 45:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 22:
                        case 54:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 208:
                        case 209:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 104:
                        case 108:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 11:
                        case 139:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 19:
                        case 51:
                            {
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp, w[5], w[4]);
                                    Common.Interp1(dp + 1, w[5], w[3]);
                                }
                                else
                                {
                                    Common.Interp6(dp, w[5], w[2], w[4]);
                                    Common.Interp9(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 146:
                        case 178:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[3]);
                                    Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                }
                                else
                                {
                                    Common.Interp9(dp + 1, w[5], w[2], w[6]);
                                    Common.Interp6(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                break;
                            }
                        case 84:
                        case 85:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[2]);
                                    Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                }
                                else
                                {
                                    Common.Interp6(dp + 1, w[5], w[6], w[2]);
                                    Common.Interp9(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                break;
                            }
                        case 112:
                        case 113:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[4]);
                                    Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                }
                                else
                                {
                                    Common.Interp6(dp + dpL, w[5], w[8], w[4]);
                                    Common.Interp9(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 200:
                        case 204:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[7]);
                                    Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                }
                                else
                                {
                                    Common.Interp9(dp + dpL, w[5], w[8], w[4]);
                                    Common.Interp6(dp + dpL + 1, w[5], w[8], w[6]);
                                }
                                break;
                            }
                        case 73:
                        case 77:
                            {
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp, w[5], w[2]);
                                    Common.Interp1(dp + dpL, w[5], w[7]);
                                }
                                else
                                {
                                    Common.Interp6(dp, w[5], w[4], w[2]);
                                    Common.Interp9(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 42:
                        case 170:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    Common.Interp1(dp, w[5], w[1]);
                                    Common.Interp1(dp + dpL, w[5], w[8]);
                                }
                                else
                                {
                                    Common.Interp9(dp, w[5], w[4], w[2]);
                                    Common.Interp6(dp + dpL, w[5], w[4], w[8]);
                                }
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 14:
                        case 142:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    Common.Interp1(dp, w[5], w[1]);
                                    Common.Interp1(dp + 1, w[5], w[6]);
                                }
                                else
                                {
                                    Common.Interp9(dp, w[5], w[4], w[2]);
                                    Common.Interp6(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 67:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 70:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                Common.Interp1(dp + 1, w[5], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 28:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 152:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 194:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 98:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 56:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 25:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 26:
                        case 31:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 82:
                        case 214:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 88:
                        case 248:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 74:
                        case 107:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 27:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp1(dp + 1, w[5], w[3]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 86:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                break;
                            }
                        case 216:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                Common.Interp1(dp + dpL, w[5], w[7]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 106:
                            {
                                Common.Interp1(dp, w[5], w[1]);
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 30:
                            {
                                Common.Interp1(dp, w[5], w[1]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 210:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                Common.Interp1(dp + 1, w[5], w[3]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 120:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                break;
                            }
                        case 75:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[7]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 29:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 198:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                Common.Interp1(dp + 1, w[5], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 184:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 99:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 57:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 71:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                Common.Interp1(dp + 1, w[5], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 156:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 226:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 60:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 195:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 102:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                Common.Interp1(dp + 1, w[5], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 153:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 58:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    Common.Interp1(dp, w[5], w[1]);
                                }
                                else
                                {
                                    Common.Interp7(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[3]);
                                }
                                else
                                {
                                    Common.Interp7(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 83:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[3]);
                                }
                                else
                                {
                                    Common.Interp7(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 92:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[7]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 202:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    Common.Interp1(dp, w[5], w[1]);
                                }
                                else
                                {
                                    Common.Interp7(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[7]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 78:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    Common.Interp1(dp, w[5], w[1]);
                                }
                                else
                                {
                                    Common.Interp7(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp1(dp + 1, w[5], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[7]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 154:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    Common.Interp1(dp, w[5], w[1]);
                                }
                                else
                                {
                                    Common.Interp7(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[3]);
                                }
                                else
                                {
                                    Common.Interp7(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 114:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[3]);
                                }
                                else
                                {
                                    Common.Interp7(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 89:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[7]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 90:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    Common.Interp1(dp, w[5], w[1]);
                                }
                                else
                                {
                                    Common.Interp7(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[3]);
                                }
                                else
                                {
                                    Common.Interp7(dp + 1, w[5], w[2], w[6]);
                                }
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[7]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 55:
                        case 23:
                            {
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp, w[5], w[4]);
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp6(dp, w[5], w[2], w[4]);
                                    Common.Interp9(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 182:
                        case 150:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                    Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                }
                                else
                                {
                                    Common.Interp9(dp + 1, w[5], w[2], w[6]);
                                    Common.Interp6(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                break;
                            }
                        case 213:
                        case 212:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[2]);
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp6(dp + 1, w[5], w[6], w[2]);
                                    Common.Interp9(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                break;
                            }
                        case 241:
                        case 240:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[4]);
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp6(dp + dpL, w[5], w[8], w[4]);
                                    Common.Interp9(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 236:
                        case 232:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                    Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                }
                                else
                                {
                                    Common.Interp9(dp + dpL, w[5], w[8], w[4]);
                                    Common.Interp6(dp + dpL + 1, w[5], w[8], w[6]);
                                }
                                break;
                            }
                        case 109:
                        case 105:
                            {
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp, w[5], w[2]);
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp6(dp, w[5], w[4], w[2]);
                                    Common.Interp9(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 171:
                        case 43:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                    Common.Interp1(dp + dpL, w[5], w[8]);
                                }
                                else
                                {
                                    Common.Interp9(dp, w[5], w[4], w[2]);
                                    Common.Interp6(dp + dpL, w[5], w[4], w[8]);
                                }
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 143:
                        case 15:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                    Common.Interp1(dp + 1, w[5], w[6]);
                                }
                                else
                                {
                                    Common.Interp9(dp, w[5], w[4], w[2]);
                                    Common.Interp6(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 124:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                break;
                            }
                        case 203:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[7]);
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 62:
                            {
                                Common.Interp1(dp, w[5], w[1]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 211:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                Common.Interp1(dp + 1, w[5], w[3]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 118:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                break;
                            }
                        case 217:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                Common.Interp1(dp + dpL, w[5], w[7]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 110:
                            {
                                Common.Interp1(dp, w[5], w[1]);
                                Common.Interp1(dp + 1, w[5], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 155:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp1(dp + 1, w[5], w[3]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 188:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 185:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 61:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 157:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 103:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                Common.Interp1(dp + 1, w[5], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 227:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 230:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                Common.Interp1(dp + 1, w[5], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 199:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                Common.Interp1(dp + 1, w[5], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 220:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[7]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 158:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    Common.Interp1(dp, w[5], w[1]);
                                }
                                else
                                {
                                    Common.Interp7(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 234:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    Common.Interp1(dp, w[5], w[1]);
                                }
                                else
                                {
                                    Common.Interp7(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 242:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[3]);
                                }
                                else
                                {
                                    Common.Interp7(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 59:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[3]);
                                }
                                else
                                {
                                    Common.Interp7(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 121:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 87:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 79:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp1(dp + 1, w[5], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[7]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 122:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    Common.Interp1(dp, w[5], w[1]);
                                }
                                else
                                {
                                    Common.Interp7(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[3]);
                                }
                                else
                                {
                                    Common.Interp7(dp + 1, w[5], w[2], w[6]);
                                }
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 94:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    Common.Interp1(dp, w[5], w[1]);
                                }
                                else
                                {
                                    Common.Interp7(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[7]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 218:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    Common.Interp1(dp, w[5], w[1]);
                                }
                                else
                                {
                                    Common.Interp7(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[3]);
                                }
                                else
                                {
                                    Common.Interp7(dp + 1, w[5], w[2], w[6]);
                                }
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[7]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 91:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[3]);
                                }
                                else
                                {
                                    Common.Interp7(dp + 1, w[5], w[2], w[6]);
                                }
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[7]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 229:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 167:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                Common.Interp1(dp + 1, w[5], w[6]);
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 173:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 181:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 186:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    Common.Interp1(dp, w[5], w[1]);
                                }
                                else
                                {
                                    Common.Interp7(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[3]);
                                }
                                else
                                {
                                    Common.Interp7(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 115:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[3]);
                                }
                                else
                                {
                                    Common.Interp7(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 93:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[7]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 206:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    Common.Interp1(dp, w[5], w[1]);
                                }
                                else
                                {
                                    Common.Interp7(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp1(dp + 1, w[5], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[7]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 205:
                        case 201:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[7]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 174:
                        case 46:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    Common.Interp1(dp, w[5], w[1]);
                                }
                                else
                                {
                                    Common.Interp7(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp1(dp + 1, w[5], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 179:
                        case 147:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[3]);
                                }
                                else
                                {
                                    Common.Interp7(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 117:
                        case 116:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                }
                                else
                                {
                                    Common.Interp7(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 189:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 231:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                Common.Interp1(dp + 1, w[5], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 126:
                            {
                                Common.Interp1(dp, w[5], w[1]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                break;
                            }
                        case 219:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp1(dp + 1, w[5], w[3]);
                                Common.Interp1(dp + dpL, w[5], w[7]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 125:
                            {
                                if (Common.Diff(w[8], w[4]))
                                {
                                    Common.Interp1(dp, w[5], w[2]);
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp6(dp, w[5], w[4], w[2]);
                                    Common.Interp9(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp1(dp + 1, w[5], w[2]);
                                Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                break;
                            }
                        case 221:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + 1, w[5], w[2]);
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp6(dp + 1, w[5], w[6], w[2]);
                                    Common.Interp9(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[7]);
                                break;
                            }
                        case 207:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                    Common.Interp1(dp + 1, w[5], w[6]);
                                }
                                else
                                {
                                    Common.Interp9(dp, w[5], w[4], w[2]);
                                    Common.Interp6(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[7]);
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 238:
                            {
                                Common.Interp1(dp, w[5], w[1]);
                                Common.Interp1(dp + 1, w[5], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                    Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                }
                                else
                                {
                                    Common.Interp9(dp + dpL, w[5], w[8], w[4]);
                                    Common.Interp6(dp + dpL + 1, w[5], w[8], w[6]);
                                }
                                break;
                            }
                        case 190:
                            {
                                Common.Interp1(dp, w[5], w[1]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                    Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                }
                                else
                                {
                                    Common.Interp9(dp + 1, w[5], w[2], w[6]);
                                    Common.Interp6(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                break;
                            }
                        case 187:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                    Common.Interp1(dp + dpL, w[5], w[8]);
                                }
                                else
                                {
                                    Common.Interp9(dp, w[5], w[4], w[2]);
                                    Common.Interp6(dp + dpL, w[5], w[4], w[8]);
                                }
                                Common.Interp1(dp + 1, w[5], w[3]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 243:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                Common.Interp1(dp + 1, w[5], w[3]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    Common.Interp1(dp + dpL, w[5], w[4]);
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp6(dp + dpL, w[5], w[8], w[4]);
                                    Common.Interp9(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 119:
                            {
                                if (Common.Diff(w[2], w[6]))
                                {
                                    Common.Interp1(dp, w[5], w[4]);
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp6(dp, w[5], w[2], w[4]);
                                    Common.Interp9(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                break;
                            }
                        case 237:
                        case 233:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 175:
                        case 47:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp1(dp + 1, w[5], w[6]);
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                break;
                            }
                        case 183:
                        case 151:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 245:
                        case 244:
                            {
                                Common.Interp2(dp, w[5], w[4], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 250:
                            {
                                Common.Interp1(dp, w[5], w[1]);
                                Common.Interp1(dp + 1, w[5], w[3]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 123:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp1(dp + 1, w[5], w[3]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                break;
                            }
                        case 95:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[7]);
                                Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                break;
                            }
                        case 222:
                            {
                                Common.Interp1(dp, w[5], w[1]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[7]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 252:
                            {
                                Common.Interp2(dp, w[5], w[1], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 249:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp2(dp + 1, w[5], w[3], w[2]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 235:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp2(dp + 1, w[5], w[3], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 111:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp1(dp + 1, w[5], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[6]);
                                break;
                            }
                        case 63:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp2(dp + dpL + 1, w[5], w[9], w[8]);
                                break;
                            }
                        case 159:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[7], w[8]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 215:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp2(dp + dpL, w[5], w[7], w[4]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 246:
                            {
                                Common.Interp2(dp, w[5], w[1], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 254:
                            {
                                Common.Interp1(dp, w[5], w[1]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 253:
                            {
                                Common.Interp1(dp, w[5], w[2]);
                                Common.Interp1(dp + 1, w[5], w[2]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 251:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp1(dp + 1, w[5], w[3]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 239:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp, w[5], w[4], w[2]);
                                }
                                Common.Interp1(dp + 1, w[5], w[6]);
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp1(dp + dpL + 1, w[5], w[6]);
                                break;
                            }
                        case 127:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + 1, w[5], w[2], w[6]);
                                }
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL, w[5], w[8], w[4]);
                                }
                                Common.Interp1(dp + dpL + 1, w[5], w[9]);
                                break;
                            }
                        case 191:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[8]);
                                Common.Interp1(dp + dpL + 1, w[5], w[8]);
                                break;
                            }
                        case 223:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[7]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp2(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 247:
                            {
                                Common.Interp1(dp, w[5], w[4]);
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + 1, w[5], w[2], w[6]);
                                }
                                Common.Interp1(dp + dpL, w[5], w[4]);
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                        case 255:
                            {
                                if (Common.Diff(w[4], w[2]))
                                {
                                    *dp = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp, w[5], w[4], w[2]);
                                }
                                if (Common.Diff(w[2], w[6]))
                                {
                                    *(dp + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + 1, w[5], w[2], w[6]);
                                }
                                if (Common.Diff(w[8], w[4]))
                                {
                                    *(dp + dpL) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + dpL, w[5], w[8], w[4]);
                                }
                                if (Common.Diff(w[6], w[8]))
                                {
                                    *(dp + dpL + 1) = w[5];
                                }
                                else
                                {
                                    Common.Interp10(dp + dpL + 1, w[5], w[6], w[8]);
                                }
                                break;
                            }
                    }
                    sp++;
                    dp += 2;
                }

                sRowP += srb;
                sp = (uint*)sRowP;

                dRowP += drb * 2;
                dp = (uint*)dRowP;
            }
        }

        public static unsafe void hq2x_32(uint[] sp, uint[] dp, int Xres, int Yres)
        {
            var rowBytesL = Xres * 4;
            fixed (uint* sp_ptr = sp, dp_ptr = dp)
            {
                hq2x_32_rb(sp_ptr, (uint)rowBytesL, dp_ptr, (uint)(rowBytesL * 2), Xres, Yres);
            }
        }
    }
}