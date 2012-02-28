namespace GBEmu.Emulator.Cartridge
{
	class MBC1 : Cart
	{
		private bool mbc1_IsMemoryModel_16_8;

		public MBC1(byte[] inFile) : base(inFile)
		{
			mbc1_IsMemoryModel_16_8 = true;
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
				RamEnabled = ((value & 0x0F) == 0x0A);
			}
			#endregion
			#region 3000-3FFF
			else if (position >= 0x3000 && position < 0x4000)
			{
				//Writes to 2000-3FFF will:
				//MBC1: Write ROM Bank number ***BBBBB (0 => 1)
				if (value == 0)
				{
					bankNum = 1;
				}
				else bankNum = (value & 0x1F);
			}
			#endregion
			#region 4000-5FFF
			else if (position < 0x6000)
			{
				//Writes to 4000-5FFF will:
				//MBC1: Write RAM Bank number ******BB (4/32)
				//MBC1: Write ROM upper bank num ******BB (16/8)
				externalRamBank = (byte)(value & 0x3);
			}
			#endregion
			#region 6000-7FFF
			else if (position < 0x8000 && RamEnabled)
			{
				//Writes to 6000-7FFF will:
				//MBC1: Change memory model from 16/8 to 4/32
				mbc1_IsMemoryModel_16_8 = ((value & 1) == 0);
			}
			#endregion
			#region A000-BFFF
			else if (position > 0x9FFF & position < 0xC000 & RamEnabled)
			{
				externalRamMap[externalRamBank, position - 0xA000] = value;
			}
			#endregion
		}
	}
}
