namespace GBEmu.Emulator.Graphics
{
    public interface IRenderable
    {
        bool isDebugEnabled();

        void CopyFrameData(uint[] newData);

        void CopyTileData(uint[] newData);
    }
}