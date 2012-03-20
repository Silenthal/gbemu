using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XnaColor = Microsoft.Xna.Framework.Color;
using GdiColor = System.Drawing.Color;


namespace GBEmu.Render.XNA
{
	public class XNARenderWindow : GraphicsDeviceControl, IRenderable
	{
		Texture2D[] canvasBuffer;
		int canvasBufferIndex;
		int canvasScreenIndex;
		SpriteBatch spriteBatch;

		protected override void Initialize()
		{
			canvasBuffer = new Texture2D[2];
			canvasBuffer[0] = new Texture2D(GraphicsDevice, 160, 144, false, SurfaceFormat.Color);
			canvasBuffer[1] = new Texture2D(GraphicsDevice, 160, 144, false, SurfaceFormat.Color);
			canvasBufferIndex = 0;
			canvasScreenIndex = 1;
			spriteBatch = new SpriteBatch(GraphicsDevice);
			GraphicsDevice.RasterizerState = RasterizerState.CullNone;
		}

		protected override void Draw()
		{
			GraphicsDevice.Clear(XnaColor.CornflowerBlue);
			spriteBatch.Begin(SpriteSortMode.Immediate, null);
			spriteBatch.Draw(canvasBuffer[canvasScreenIndex], GraphicsDevice.Viewport.Bounds, canvasBuffer[canvasScreenIndex].Bounds, Color.White);
			spriteBatch.End();
			GraphicsDevice.Textures[0] = null;
		}

		public void CopyFrameData(uint[] newData)
		{
			canvasBuffer[canvasBufferIndex].SetData<uint>(newData);
		}

		private void SwapBuffer()
		{
			int temp = canvasBufferIndex;
			canvasBufferIndex = canvasScreenIndex;
			canvasScreenIndex = temp;
			GraphicsDevice.Textures[0] = null;
		}

		public void BlitScreen()
		{
			SwapBuffer();
			Invalidate();
		}
	}
}
