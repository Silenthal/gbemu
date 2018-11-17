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

        private int _bank1 = 1;
        private int _bank2 = 0;

        private int Bank1 { get => _bank1; set { _bank1 = value & 0x1F; if (_bank1 == 0) { _bank1++; } } }
        private int Bank2 { get => _bank2; set { if ((value & 0x3) < MaxRamBank) _bank2 = value & 0x03; } }

        private int ActualRomBank => Bank1 | (Bank2 << 5);

        public MBC1(byte[] inFile, CartFeatures cartFeatures)
            : base(inFile, cartFeatures)
        {
            RamBankMode = false;
        }

        public override byte Read(int position)
        {
            // For MBC1, if the special ram mode is enabled, reads to bank 0
            // will be diverted to the bank specified by bank2 << 5
            if (RamBankMode && position < 0x4000 && MaxRomBank > 32)
            {
                return romFile[(Bank2 << (14 + 5)) | (position & 0x3FFF)];
            }
            else
            {
                return base.Read(position);
            }
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
                    Bank1 = value;
                    RomBank = ActualRomBank;
                    Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, position, $"Bank changed to ${RomBank:X}"));
                    break;

                case 2://0x4000 - 0x5FFF
                    Bank2 = value;
                    RomBank = ActualRomBank;
                    if (RamBankMode)
                    {
                        CartRamBank = Bank2;
                        Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, position, $"In Ram Bank Mode, Cart Ram Bank changed to ${CartRamBank:X}"));
                    }
                    else
                    {
                        Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, position, $"In regular mode, Bank changed to ${RomBank:X}"));
                    }
                    break;

                case 3://0x6000 - 0x7FFF
                    RamBankMode = ((value & 1) != 0);
                    Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, position, $"RAM Bank Mode {(RamBankMode ? "Enabled" : "Disabled")}."));
                    break;
            }
        }

        protected override byte CartRamRead(int position)
        {
            position -= 0xA000;
            if (RamEnabled)
            {
                if (RamBankMode)
                {
                    return CartRam[Bank2 << 13 | position];
                }
                else
                {
                    return CartRam[position];
                }
            }
            else
            {
                Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, position, "Disabled RAM Read Attempt"));
                return 0xFF;
            }
        }

        protected override void CartRamWrite(int position, byte value)
        {
            position -= 0xA000;
            if (RamEnabled)
            {
                if (RamBankMode)
                {
                    CartRam[Bank2 << 13 | position] = value;
                }
                else
                {
                    CartRam[position] = value;
                }
            }
            else
            {
                Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, position, $"Disabled RAM Write Attempt[{value:X2}]."));
            }
        }
    }
}