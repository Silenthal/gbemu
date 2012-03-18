namespace GBEmu.Emulator.Cartridge
{
	class PlainCart : Cart
	{
		public PlainCart(byte[] romFile, CartFeatures cartFeatures)
			: base(romFile, cartFeatures)
		{

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
		}
	}
}
