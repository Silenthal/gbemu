using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GBEmu.Render
{
    class Scaler
    {
        public static void ScaleImage(uint[] inputImage, uint[] outputImage, int baseWidth, int baseHeight, ScaleType baseScale)
        {
            switch (baseScale)
            {
                case ScaleType.Hq2x:
                    {
                        Hq2x.hq2x_32(inputImage, outputImage, baseWidth, baseHeight);
                        break;
                    }
                case ScaleType.Hq3x:
                    {
                        Hq3x.hq3x_32(inputImage, outputImage, baseWidth, baseHeight);
                        break;
                    }
                case ScaleType.TwoX:
                    {
                        for (int y = 0; y < baseHeight; y++)
                        {
                            for (int x = 0; x < baseWidth; x++)
                            {
                                outputImage[(((y * 2) + 0) * baseWidth * 2) + (x * 2) + 0] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 2) + 0) * baseWidth * 2) + (x * 2) + 1] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 2) + 1) * baseWidth * 2) + (x * 2) + 0] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 2) + 1) * baseWidth * 2) + (x * 2) + 1] = inputImage[(y * baseWidth) + x];
                            }
                        }
                        break;
                    }
                case ScaleType.ThreeX:
                    {
                        for (int y = 0; y < baseHeight; y++)
                        {
                            for (int x = 0; x < baseWidth; x++)
                            {
                                outputImage[(((y * 3) + 0) * baseWidth * 3) + (x * 3) + 0] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 3) + 0) * baseWidth * 3) + (x * 3) + 1] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 3) + 0) * baseWidth * 3) + (x * 3) + 2] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 3) + 1) * baseWidth * 3) + (x * 3) + 0] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 3) + 1) * baseWidth * 3) + (x * 3) + 1] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 3) + 1) * baseWidth * 3) + (x * 3) + 2] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 3) + 2) * baseWidth * 3) + (x * 3) + 0] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 3) + 2) * baseWidth * 3) + (x * 3) + 1] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 3) + 2) * baseWidth * 3) + (x * 3) + 2] = inputImage[(y * baseWidth) + x];
                            }
                        }
                        break;
                    }
                case ScaleType.FourX:
                    {
                        for (int y = 0; y < baseHeight; y++)
                        {
                            for (int x = 0; x < baseWidth; x++)
                            {
                                outputImage[(((y * 4) + 0) * baseWidth * 4) + (x * 4) + 0] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 4) + 0) * baseWidth * 4) + (x * 4) + 1] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 4) + 0) * baseWidth * 4) + (x * 4) + 2] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 4) + 0) * baseWidth * 4) + (x * 4) + 3] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 4) + 1) * baseWidth * 4) + (x * 4) + 0] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 4) + 1) * baseWidth * 4) + (x * 4) + 1] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 4) + 1) * baseWidth * 4) + (x * 4) + 2] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 4) + 1) * baseWidth * 4) + (x * 4) + 3] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 4) + 2) * baseWidth * 4) + (x * 4) + 0] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 4) + 2) * baseWidth * 4) + (x * 4) + 1] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 4) + 2) * baseWidth * 4) + (x * 4) + 2] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 4) + 2) * baseWidth * 4) + (x * 4) + 3] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 4) + 3) * baseWidth * 4) + (x * 4) + 0] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 4) + 3) * baseWidth * 4) + (x * 4) + 1] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 4) + 3) * baseWidth * 4) + (x * 4) + 2] = inputImage[(y * baseWidth) + x];
                                outputImage[(((y * 4) + 3) * baseWidth * 4) + (x * 4) + 3] = inputImage[(y * baseWidth) + x];
                            }
                        }
                        break;
                    }
                default:
                    {
                        for (int i = 0; i < inputImage.Length; i++)
                        {
                            outputImage[i] = inputImage[i];
                        }
                        break;
                    }
            }
        }
    }
}
