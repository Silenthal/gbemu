namespace GBEmu.Emulator.Cartridge
{
	class MBC4 : Cart
	{
		public MBC4(byte[] romFile) : base(romFile)
		{

		}

		protected override void InitializeOutsideRAM()
		{
			if (RamPresent)
			{
				switch (romFile[0x149])
				{
					case 0x01:
						externalRamMap = new byte[1, 0x800];//A000-A7FF, 2 kilobytes
						break;
					case 0x02:
						externalRamMap = new byte[1, 0x2000];//A000-BFFF, 8 kilobytes
						break;
					case 0x03:
						externalRamMap = new byte[4, 0x2000];//A000-BFFF x 4, 32 kilobytes
						break;
					default:
						externalRamMap = new byte[0, 0];
						break;
				}
			}
			else
			{
				externalRamMap = new byte[0, 0];
			}
		}

		public override void Write(int position, byte value)
		{
			#region 0000-1FFF
			if (position < 0x2000)
			{

			}
			#endregion
			#region 2000-2FFF
			else if (position < 0x3000)
			{

			}
			#endregion
			#region 3000-3FFF
			else if (position < 0x4000)
			{
				//Writes to 2000-3FFF will:
				//MBC1: Write ROM Bank number ***BBBBB (0 => 1)
				//MBC2: Write ROM Bank number, if addr & 0x100 is 1 ****BBBB
				//MBC3: Write ROM Bank number *BBBBBBB
				//MBC5: Write ROM Low Bank number (if write to 2000-2FFF) BBBBBBBB
				//MBC5: Write ROM High Bank number (if write to 3000-3FFF) *******B
				bankNum = value & 0x0F;
			}
			#endregion
			#region 4000-5FFF
			else if (position < 0x6000)
			{
				//Writes to 4000-5FFF will:
				//MBC1: Write RAM Bank number ******BB (4/32)
				//MBC1: Write ROM upper bank num ******BB (16/8)
				//MBC5: Write RAM Bank number ****BBBB (no rumble present)
				//MBC5: Write RAM Bank number + enable/disable rumble ****MBBB (M = 0, 1 for off/on) (B for bank)
				//MBC3: Write RTC register number if val is >= 0x8
				//MBC3: Write RAM Bank number ******BB
			}
			#endregion
			#region 6000-7FFF
			else if (position < 0x8000 && RamEnabled)
			{
				//Writes to 6000-7FFF will:
				//MBC1: Change memory model from 16/8 to 4/32
				//MBC5: Control latch of RTC
			}
			#endregion
			#region A000-BFFF
			else if (position > 0x9FFF & position < 0xC000 & RamEnabled)
			{
				//Possibly add another condition for HuC1
				//Writes to A000-BFFF will:
				//MBC1: 
				externalRamMap[externalRamBank, position - 0xA000] = value;
			}
			#endregion
		}
	}
}
