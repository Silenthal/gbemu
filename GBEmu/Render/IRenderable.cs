namespace GBEmu.Render
{
	interface IRenderable
	{
		void CopyData(Microsoft.Xna.Framework.Color[] newData);
		void RenderFrame();
	}
}
