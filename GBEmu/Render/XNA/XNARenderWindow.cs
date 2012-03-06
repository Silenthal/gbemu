﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XNAColor = Microsoft.Xna.Framework.Color;
using GDIColor = System.Drawing.Color;


namespace GBEmu.Render.XNA
{
	public class XNARenderWindow : GraphicsDeviceControl, IRenderable
	{
		Texture2D canvas;
		SpriteBatch spriteBatch;

		protected override void Initialize()
		{
			canvas = new Texture2D(GraphicsDevice, 160, 144, false, SurfaceFormat.Color);
			spriteBatch = new SpriteBatch(GraphicsDevice);
			GraphicsDevice.RasterizerState = RasterizerState.CullNone;
		}

		protected override void Draw()
		{
			GraphicsDevice.Clear(XNAColor.CornflowerBlue);
			spriteBatch.Begin(SpriteSortMode.Immediate, null);
			spriteBatch.Draw(canvas, GraphicsDevice.Viewport.Bounds, canvas.Bounds, Color.White);
			spriteBatch.End();
			GraphicsDevice.Textures[0] = null;
		}

		public void CopyData(int[] newData)
		{
			canvas.SetData<int>(newData);
		}

		public void RenderFrame()
		{
			Invalidate();
		}
	}
}