using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GBEmu.Render.Gdi
{
	public class GdiWindow : Control, IRenderable
	{
		Bitmap bx;
		public int[] buffer;

		protected override void OnCreateControl()
		{
			buffer = new int[160 * 144];
			bx = new System.Drawing.Bitmap(160, 144, PixelFormat.Format32bppArgb);
			SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
			Application.Idle += delegate { Invalidate(); };
			base.OnCreateControl();
		}

		public void CopyData(int[] newData)
		{
			lock (buffer)
			{
				Array.Copy(newData, buffer, buffer.Length);
			}
		}

		public void RenderFrame()
		{
			lock (bx)
			{
				BitmapData bmData = bx.LockBits(new Rectangle(0, 0, bx.Width, bx.Height), ImageLockMode.WriteOnly, bx.PixelFormat);
				IntPtr xrg_start = bmData.Scan0;
				Marshal.Copy(buffer, 0, xrg_start, buffer.Length);
				bx.UnlockBits(bmData);
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
			e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
			e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
			e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
			e.Graphics.DrawImageUnscaled(bx, 0, 0);
			base.OnPaint(e);
		}
	}
}
