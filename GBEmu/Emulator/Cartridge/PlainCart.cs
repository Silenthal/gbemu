namespace GBEmu.Emulator.Cartridge
{
	class PlainCart : Cart
	{
		public PlainCart(byte[] romFile) : base(romFile)
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
						features ^= CartFeatures.RAM;
						break;
				}
			}
			else
			{
				externalRamMap = new byte[0, 0];
				features ^= CartFeatures.RAM;
			}
		}

		public override void Write(int position, byte value)
		{
			if (position >= 0)
			{
				if (position < 0x8000)
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
				else
				{
					CartRamWrite(position, value);
				}
			}
		}
	}
}
