namespace GBEmu.Emulator.Cartridge
{
	internal class MBC2 : Cart
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
			switch (position >> 13)
			{
				case 0://0x0000 - 0x1FFF
					RamEnabled = (value & 0xF) == 0xA;
					break;
				case 1://0x2000 - 0x3FFF
					//MBC2: Write ROM Bank number, if addr & 0x100 is 1
					if ((position & 0x100) != 0)
					{
						RomBank = value & 0x0F;
					}
					break;
			}
		}
	}
}
