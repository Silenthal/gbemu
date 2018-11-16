using GBEmu.Emulator.Debug;

namespace GBEmu.Emulator.Cartridge
{
    public class MBC1 : Cart
    {
        /// <summary>
        /// Determines whether RAM bank addressing mode is to be used.
        /// </summary>
        /// <remarks>
        /// The MBC1 has two addressing modes, that allow for different types of memory banking:
        /// 1. 4mBit ROM / 32 kB RAM
        /// This mode allows for the addressing of 4 megabits (2 ^ 5 * 0x4000 bytes) of ROM,
        /// and 32 kilobytes (2 ^ 2 * 0x2000 bytes) of RAM.
        ///
        /// 2. 16 mBit ROM / 8 kB RAM
        /// This mode allows for the addressing of 16 megabits (2 ^ 7 * 0x4000 bytes) of ROM,
        /// and 8 kilobytes (1 * 0x2000 bytes) of RAM.
        ///
        /// The default mode on startup is 16/8.
        /// </remarks>
        private bool RamBankMode;

        public MBC1(byte[] inFile, CartFeatures cartFeatures)
            : base(inFile, cartFeatures)
        {
            RamBankMode = false;
        }

        protected override void MBCWrite(int position, byte value)
        {
            switch (position >> 13)
            {
                case 0://0x0000 - 0x1FFF
                    RamEnabled = (value & 0xF) == 0xA;
                    Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, position, $"Cart RAM {(RamEnabled ? "Enabled" : "Disabled")}."));
                    break;

                case 1://0x2000 - 0x3FFF
                    if (RamBankMode)
                    {
                        RomBank = (value & 0x1F);
                        Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, position, $"In Ram Bank Mode, Bank changed to ${RomBank:X}"));
                    }
                    else
                    {
                        RomBank = (RomBank & 0x60) | (value & 0x1F);
                        Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, position, $"In regular mode, Bank changed to ${RomBank:X}"));
                    }
                    if (RomBank == 0 || RomBank == 0x20 || RomBank == 0x40 || RomBank == 0x60)
                    {
                        // Whenever banks $0, $20, $40, or $60 are selected, the one right after will be loaded
                        RomBank = RomBank + 1;
                        Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, position, $"Bank updated to ${RomBank:X}"));
                    }
                    break;

                case 2://0x4000 - 0x5FFF
                    if (RamBankMode)
                    {
                        CartRamBank = (byte)(value & 0x03);
                        Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, position, $"In Ram Bank Mode, Cart Ram Bank changed to ${CartRamBank:X}"));
                    }
                    else
                    {
                        RomBank = ((value & 0x03) << 5) | (RomBank & 0x1F);
                        Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, position, $"In regular mode, Bank changed to ${RomBank:X}"));
                    }
                    break;

                case 3://0x6000 - 0x7FFF
                    RamBankMode = ((value & 1) != 0);
                    Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, position, $"RAM Bank Mode {(RamBankMode ? "Enabled" : "Disabled")}."));
                    break;
            }
        }
    }
}