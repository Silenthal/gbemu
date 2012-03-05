namespace GBEmu.Emulator
{
    class Serial : TimedIODevice
    {
        public byte SerialInterrupt { get { return 0; } }

        public Serial()
        {

        }

        public override byte Read(int position)
        {
            return 0;
        }

        public override void Write(int position, byte data)
        {
            
        }

        public override void UpdateCounter(int cycles)
        {
            
        }
    }
}
