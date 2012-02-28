namespace GBEmu.Emulator.Cartridge
{
	class MBC5 : Cart
	{
		private bool IsRumble;

		public MBC5(byte[] inFile) : base(inFile)
		{
			IsRumble = false;
		}

		protected override void MBCWrite(int position, byte value)
		{
			#region 0000-1FFF
			if (position < 0x2000)
			{
				//xxxx0101 == on
				//xxxx0000 == off
				RamEnabled = ((value & 0x0F) == 0x0A);
			}
			#endregion
			#region 2000-2FFF
			else if (position < 0x3000)
			{
				//_______* BBBBBBBB
				RomBank = (RomBank & 0x100) | value;
			}
			#endregion
			#region 3000-3FFF
			else if (position < 0x4000)
			{
				//_______B ********
				RomBank = RomBank & 0xFF | ((value & 1) << 8);
			}
			#endregion
			#region 4000-5FFF
			else if (position < 0x6000)
			{
				//Writes to 4000-5FFF will:
				//MBC5: Write RAM Bank number ****BBBB (no rumble present)
				//MBC5: Write RAM Bank number + enable/disable rumble ****MBBB (M = 0, 1 for off/on) (B for bank)
				if (RumblePresent)
				{
					CartRamBank = (byte)(value & 0x07);
					IsRumble = (value & 0x08) == 1;
				}
				else CartRamBank = (byte)(value & 0x0F);
			}
			#endregion
		}
	}
}
