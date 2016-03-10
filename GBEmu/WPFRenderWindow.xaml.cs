using GBEmu.Emulator.Graphics;
using GBEmu.Render;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GBEmu
{
    /// <summary>
    /// Interaction logic for WPFRenderWindow.xaml
    /// </summary>
    public partial class WPFRenderWindow : UserControl, IRenderable
    {
        private WriteableBitmap screenBuffer;
        private uint[] backBuffer;
        private uint[] frontBuffer;
        private Int32Rect updateRegion;
        private int stride;
        private ScaleType baseScale;
        private int baseWidth;
        private int baseHeight;
        private uint[] tiles;
        private WPFRenderWindow tileMapScreen;

        public WPFRenderWindow()
        {
            InitializeComponent();
        }

        public void InitializeWindow(int width, int height, ScaleType scale = ScaleType.None)
        {
            baseWidth = width;
            baseHeight = height;
            backBuffer = new uint[baseWidth * baseHeight];
            tiles = new uint[16 * 24 * 8 * 8];
            InitializeScale(scale);
        }

        public void RegisterTilemap(WPFRenderWindow wps)
        {
            tileMapScreen = wps;
        }

        public void UnregisterTilemap()
        {
            tileMapScreen = null;
        }

        public void InitializeScale(ScaleType scale)
        {
            var scaleFactor = 1;
            switch (scale)
            {
                case ScaleType.TwoX:
                case ScaleType.Hq2x:
                    scaleFactor = 2;
                    break;

                case ScaleType.ThreeX:
                case ScaleType.Hq3x:
                    scaleFactor = 3;
                    break;

                case ScaleType.FourX:
                case ScaleType.Hq4x:
                    scaleFactor = 4;
                    break;

                default:
                    break;
            }
            baseScale = scale;
            screenBuffer = new WriteableBitmap(baseWidth * scaleFactor, baseHeight * scaleFactor, 96, 96, PixelFormats.Bgra32, null);
            updateRegion = new Int32Rect(0, 0, screenBuffer.PixelWidth, screenBuffer.PixelHeight);
            stride = screenBuffer.PixelWidth * 4;
            backBuffer = new uint[baseWidth * baseHeight];
            frontBuffer = new uint[screenBuffer.PixelWidth * screenBuffer.PixelHeight];
            renderWindow.Source = screenBuffer;
        }

        public void CopyFrameData(uint[] nextFrame)
        {
            for (int i = 0; i < nextFrame.Length; i++)
            {
                backBuffer[i] = nextFrame[i];
            }
            Scaler.ScaleImage(backBuffer, frontBuffer, baseWidth, baseHeight, baseScale);
            Dispatcher.BeginInvoke(new BlitDelegate(Blit));
        }

        public void CopyTileData(uint[] newData)
        {
            Array.Copy(newData, tiles, newData.Length);
        }

        public uint[] GetTileData()
        {
            return tiles;
        }

        private delegate void BlitDelegate();

        private void Blit()
        {
            screenBuffer.WritePixels(updateRegion, frontBuffer, stride, 0);
            if (tileMapScreen != null)
            {
                tileMapScreen.CopyFrameData(tiles);
            }
        }

        public bool isDebugEnabled()
        {
            return tileMapScreen != null;
        }
    }
}