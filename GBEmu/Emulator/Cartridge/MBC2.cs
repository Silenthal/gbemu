namespace GBEmu.Emulator.Cartridge
{
    public class MBC2 : Cart
    {
        public MBC2(byte[] romFile, CartFeatures cartFeatures)
            : base(romFile, cartFeatures)
        {
        }

        protected override void InitializeOutsideRAM()
        {
            MaxRamBank = 1;
            CartRam = new byte[0x200];// A000-A1FF, lower nibble only
        }

        protected override void MBCWrite(int position, byte value)
        {
            if (position < 0x4000)
            {
                var selector = position & 0x100;
                if (selector == 0)
                {
                    RamEnabled = (value & 0xF) == 0xA;
                }
                else
                {
                    int writeVal = value & 0xF;
                    if (writeVal == 0)
                    {
                        writeVal++;
                    }
                    RomBank = writeVal;
                }
            }
        }

        protected override byte CartRamRead(int position)
        {
            if (RamEnabled)
            {
                int readPos = position & 0x1FF;
                int readVal = CartRam[readPos] | 0xF0;
                return (byte)readVal;
            }
            else
            {
                return 0xFF;
            }
        }

        protected override void CartRamWrite(int position, byte value)
        {
            if (RamEnabled)
            {
                int writePos = position & 0x1FF;
                int writeVal = value & 0xF;
                CartRam[writePos] = (byte)writeVal;
            }
        }
    }
}