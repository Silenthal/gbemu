namespace GBEmu.Emulator.Graphics
{
    public interface IRenderable
    {
        bool IsDebugEnabled();

        void CopyFrameData(uint[] newData);

        void CopyTileData(uint[] newData);
    }
}