namespace GBEmu.Emulator.IO
{
    public class Serial : TimedIODevice
    {
        public Serial()
        {
        }

        public override byte Read(int position)
        {
            switch (position - 0xFF00)
            {
                case IOPorts.SB:
                    return 0x00;
                case IOPorts.SC:
                    return 0b0111_1110;
                default:
                    return 0xFF;
            }
        }

        public override void Write(int position, byte data)
        {
        }

        public override void UpdateTime(int cycles)
        {
        }
    }
}