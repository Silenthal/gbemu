namespace GBEmu.Render
{
	interface IRenderable
	{
		void CopyFrameData(uint[] newData);
		void BlitScreen();
	}
}
