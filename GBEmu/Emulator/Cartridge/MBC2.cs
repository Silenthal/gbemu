namespace GBEmu.Emulator.Cartridge
{
	class MBC2 : Cart
	{
		public MBC2(byte[] romFile) : base(romFile)
		{

		}

		protected override void InitializeOutsideRAM()
		{
			if (RamPresent)
			{
				externalRamMap = new byte[1, 0x200];//A000-A1FF, lower nibble only
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
				if ((position & 0x100) == 0)
				{
					RamEnabled = ((value & 0x0F) == 0x0A);
				}
			}
			#endregion
			#region 2000-2FFF
			else if (position < 0x3000)
			{
				
			}
			#endregion
			#region 3000-3FFF
			else if (position > 0x3000 && position < 0x4000)
			{
				//Writes to 2000-3FFF will:
				//MBC1: Write ROM Bank number ***BBBBB (0 => 1)
				//MBC2: Write ROM Bank number, if addr & 0x100 is 1 ****BBBB
				//MBC3: Write ROM Bank number *BBBBBBB
				//MBC5: Write ROM Low Bank number (if write to 2000-2FFF) BBBBBBBB
				//MBC5: Write ROM High Bank number (if write to 3000-3FFF) *******B
				if ((position & 0x100) != 0)
				{
					bankNum = value & 0x0F;
				}
			}
			#endregion
			#region 4000-5FFF
			else if (position < 0x6000)
			{

			}
			#endregion
			#region A000-BFFF
			else if (position > 0x9FFF & position < 0xC000 & RamEnabled)
			{
				if (position - 0xA000 < 0x800)
				{
					externalRamMap[0, position - 0xA000] = (byte)(value | 0xF0);
				}
			}
			#endregion
		}
	}
}
