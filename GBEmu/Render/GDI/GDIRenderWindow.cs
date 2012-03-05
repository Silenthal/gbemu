using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GBEmu.Render.Gdi
{
	class GDIRenderWindow : IRenderable
	{
		private Bitmap bx;
		private int[] ImageBuffer;
		private Control FormSurface;
		private Graphics screenGraphics;

		public GDIRenderWindow(Control Surface)
		{
			bx = new System.Drawing.Bitmap(160, 144, PixelFormat.Format24bppRgb);
			ImageBuffer = new int[160 * 144];
			for (int i = 0; i < ImageBuffer.Length; i++)
			{
				ImageBuffer[i] = 0x80;
			}
			FormSurface = Surface;
			screenGraphics = FormSurface.CreateGraphics();
			screenGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			RenderFrame();
		}

		public void CopyData(int[] buffer)
		{
			Array.Copy(buffer, ImageBuffer, buffer.Length);
		}

		public void RenderFrame()
		{
			BitmapData bmd = bx.LockBits(new Rectangle(0, 0, bx.Width, bx.Height), ImageLockMode.WriteOnly, bx.PixelFormat);
			IntPtr bmdStart = bmd.Scan0;
			Marshal.Copy(ImageBuffer, 0, bmd.Scan0, ImageBuffer.Length);
			Marshal.Copy(ImageBuffer, 0, bmdStart, ImageBuffer.Length);
			bx.UnlockBits(bmd);
			screenGraphics.DrawImage(bx, 0, 0, FormSurface.Size.Width, FormSurface.Size.Height);
			FormSurface.Invalidate();
		}
	}
}
