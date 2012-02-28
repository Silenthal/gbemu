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
				CartRam = new byte[1, 0x200];//A000-A1FF, lower nibble only
			}
			else
			{
				CartRam = new byte[0, 0];
				features ^= CartFeatures.RAM;
			}
		}

		protected override void MBCWrite(int position, byte value)
		{
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
