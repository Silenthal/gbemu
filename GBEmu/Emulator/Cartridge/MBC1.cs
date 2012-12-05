namespace GBEmu.Emulator.Cartridge
{
    using GBEmu.Emulator.Debug;
    using GBEmu.Emulator.Timing;

	internal class MBC1 : Cart
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
                    if (RamEnabled)
                    {
                        Logger.GetInstance().Log(new LogMessage() {
                            source = LogMessageSource.Cart, time = GlobalTimer.GetInstance().GetTime(), position = position.ToString("X4"), message = "Cart RAM Enabled."
                        });
                    }
                    else
                    {
                        Logger.GetInstance().Log(new LogMessage() {
                            source = LogMessageSource.Cart, time = GlobalTimer.GetInstance().GetTime(), position = position.ToString("X4"), message = "Cart RAM Disabled."
                        });
                    }
					break;
				case 1://0x2000 - 0x3FFF
					if (RamBankMode)
					{
						RomBank = (value & 0x1F);
					}
					else
					{
						RomBank = (RomBank & 0x60) | (value & 0x1F);
					}
					if (RomBank == 0)
					{
						RomBank = 1;
					}
					break;
				case 2://0x4000 - 0x5FFF
					if (RamBankMode)
					{
						CartRamBank = (byte)(value & 0x03);
					}
					else
					{
						RomBank = ((value & 0x03) << 5) | (RomBank & 0x1F);
					}
					break;
				case 3://0x6000 - 0x7FFF
					RamBankMode = ((value & 1) != 0);
                    if (RamBankMode)
                    {
                        Logger.GetInstance().Log(new LogMessage() {
                            source = LogMessageSource.Cart, time = GlobalTimer.GetInstance().GetTime(), position = position.ToString("X4"), message = "RAM Bank Mode Enabled."
                        });
                    }
                    else
                    {
                        Logger.GetInstance().Log(new LogMessage() {
                            source = LogMessageSource.Cart, time = GlobalTimer.GetInstance().GetTime(), position = position.ToString("X4"), message = "RAM Bank Mode Disabled."
                        });
                    }
					break;
			}
		}
	}
}
