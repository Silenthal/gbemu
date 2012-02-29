﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GBEmu.Render.Gdi
{
	public class GdiWindow : Control
	{
		Bitmap bx;

		public GdiWindow()
		{
			
		}

		protected override void OnCreateControl()
		{
			bx = new System.Drawing.Bitmap(160, 144, PixelFormat.Format24bppRgb);
			SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
			base.OnCreateControl();
		}

		public void CopyImageData(byte[] pixData)
		{
			lock (bx)
			{
				BitmapData bxz = bx.LockBits(new Rectangle(0, 0, bx.Width, bx.Height), ImageLockMode.WriteOnly, bx.PixelFormat);
				IntPtr xrg_start = bxz.Scan0;
				Marshal.Copy(pixData, 0, xrg_start, pixData.Length);
				bx.UnlockBits(bxz);
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
