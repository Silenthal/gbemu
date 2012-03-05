using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;
using Color = System.Drawing.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace GBEmu.Render.XNA
{
	abstract public class GraphicsDeviceControl : Control
	{
		GraphicsDeviceService graphicsDeviceService;
		ServiceContainer services = new ServiceContainer();
		public GraphicsDevice GraphicsDevice
		{
			get { return graphicsDeviceService.GraphicsDevice; }
		}
		public ServiceContainer Services
		{
			get { return services; }
		}

		#region Initialization
		protected abstract void Initialize();

		protected override void OnCreateControl()
		{
			if (!DesignMode)
			{
				graphicsDeviceService = GraphicsDeviceService.AddRef(Handle, ClientSize.Width, ClientSize.Height);
				services.AddService<IGraphicsDeviceService>(graphicsDeviceService);
				Initialize();
			}
			base.OnCreateControl();
		}

		protected override void Dispose(bool disposing)
		{
			if (graphicsDeviceService != null)
			{
				graphicsDeviceService.Release(disposing);
				graphicsDeviceService = null;
			}
			base.Dispose(disposing);
		}
		#endregion

		#region Drawing
		protected override void OnPaint(PaintEventArgs e)
		{
			string beginDrawError = BeginDraw();
			if (string.IsNullOrEmpty(beginDrawError))
			{
				Draw();
				EndDraw();
			}
			else
			{
				PaintUsingSystemDrawing(e.Graphics, beginDrawError);
			}
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{

		}

		string BeginDraw()
		{
			if (graphicsDeviceService == null)
			{
				return Text + "\n\n" + GetType();
			}
			string deviceResetError = HandleDeviceReset();
			if (!string.IsNullOrEmpty(deviceResetError))
			{
				return deviceResetError;
			}
			Viewport viewport = new Viewport(0, 0, ClientSize.Width, ClientSize.Height);
			viewport.MinDepth = 0;
			viewport.MaxDepth = 1;
			GraphicsDevice.Viewport = viewport;
			return null;
		}

		protected abstract void Draw();

		void EndDraw()
		{
			try
			{
				Rectangle sourceRectangle = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
				GraphicsDevice.Present(sourceRectangle, null, this.Handle);
			}
			catch { }
		}

		string HandleDeviceReset()
		{
			bool deviceNeedsReset = false;
			switch (GraphicsDevice.GraphicsDeviceStatus)
			{
				case GraphicsDeviceStatus.Lost:
					return "Graphics device lost";
				case GraphicsDeviceStatus.NotReset:
					deviceNeedsReset = true;
					break;
				default:
					PresentationParameters pp = GraphicsDevice.PresentationParameters;
					deviceNeedsReset = (ClientSize.Width > pp.BackBufferWidth) || (ClientSize.Height > pp.BackBufferHeight);
					break;
			}
			if (deviceNeedsReset)
			{
				try
				{
					graphicsDeviceService.ResetDevice(ClientSize.Width, ClientSize.Height);
				}
				catch (Exception e)
				{
					return "Graphics device reset failed\n\n" + e;
				}
			}
			return null;
		}

		protected virtual void PaintUsingSystemDrawing(Graphics graphics, string text)
		{
			graphics.Clear(Color.CornflowerBlue);

			using (Brush brush = new SolidBrush(Color.Black))
			{
				using (StringFormat format = new StringFormat())
				{
					format.Alignment = StringAlignment.Center;
					format.LineAlignment = StringAlignment.Center;

					graphics.DrawString(text, Font, brush, ClientRectangle, format);
				}
			}
		}
		#endregion
	}
}
