namespace GBEmu.Emulator.Cartridge
{
	internal class PlainCart : Cart
	{
		public PlainCart(byte[] romFile, CartFeatures cartFeatures)
			: base(romFile, cartFeatures)
		{

		}

		protected override void MBCWrite(int position, byte value)
		{
			switch (position >> 13)
			{
				case 0://0x0000 - 0x1FFF
					RamEnabled = (value & 0xF) == 0xA;
					break;
			}
		}
	}
}
