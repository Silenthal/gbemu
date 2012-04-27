namespace GBEmu.Emulator.Cartridge
{
	class MBC2 : Cart
	{
		public MBC2(byte[] romFile, CartFeatures cartFeatures)
			: base(romFile, cartFeatures)
		{

		}

		protected override void InitializeOutsideRAM()
		{
			MaxRamBank = 1;
			CartRam = new byte[0x200];//A000-A1FF, lower nibble only
		}

		protected override void MBCWrite(int position, byte value)
		{
			position &= 0xFFFF;
			#region 0000-1FFF
			if (position < 0x2000)
			{
				if ((position & 0x100) == 0)
				{
					//xxxx0101 == on
					//xxxx0000 == off
					RamEnabled = ((value & 0x0F) == 0x0A);
				}
			}
			#endregion
			#region 2000-3FFF
			else if (position < 0x4000)
			{
				//MBC2: Write ROM Bank number, if addr & 0x100 is 1
				//****BBBB
				if ((position & 0x100) != 0)
				{
					RomBank = value & 0x0F;
				}
			}
			#endregion
		}
	}
}
