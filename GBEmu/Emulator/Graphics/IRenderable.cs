namespace GBEmu.Emulator.Graphics
{
    public interface IRenderable
    {
        void CopyFrameData(uint[] newData);
        void CopyTileData(uint[] newData);
        void BlitScreen();
    }
}
