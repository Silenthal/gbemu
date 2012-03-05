namespace GBEmu.Render
{
	interface IRenderable
	{
		void CopyData(int[] newData);
		void RenderFrame();
	}
}
