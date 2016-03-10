namespace GBEmu.Emulator.IO
{
    internal class Serial : TimedIODevice
    {
        public Serial()
        {

        }

        public override byte Read(int position)
        {
            return 0xFF;
        }

        public override void Write(int position, byte data)
        {
            
        }

        public override void UpdateTime(int cycles)
        {
            
        }
    }
}
