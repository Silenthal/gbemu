namespace GBEmu.Emulator.Cartridge
{
    public class MBC5 : Cart
    {
        private bool IsRumble;

        public MBC5(byte[] inFile, CartFeatures cartFeatures)
            : base(inFile, cartFeatures)
        {
            IsRumble = false;
        }

        protected override void MBCWrite(int position, byte value)
        {
            switch (position >> 12)
            {
                case 0:
                case 1:
                    {
                        RamEnabled = value == 0x0A;
                        break;
                    }

                case 2:
                    {
                        //_______* BBBBBBBB
                        RomBank = (RomBank & 0x100) | value;
                        break;
                    }

                case 3:
                    {
                        //_______B ********
                        RomBank = ((value & 1) << 8) | RomBank & 0xFF;
                        break;
                    }

                case 4:
                case 5:
                    {
                        //Writes to 4000-5FFF will:
                        //MBC5: Write RAM Bank number ****BBBB (no rumble present)
                        //MBC5: Write RAM Bank number + enable/disable rumble ****MBBB (M = 0, 1 for off/on) (B for bank)
                        if (RumblePresent)
                        {
                            CartRamBank = value & 0x07;
                            IsRumble = (value & 0x08) == 1;
                        }
                        else
                        {
                            CartRamBank = value & 0x0F;
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
    }
}