namespace GBEmu.Emulator.Cartridge
{
	class MBC1 : Cart
	{
		private bool RamBankMode;

		public MBC1(byte[] inFile) : base(inFile)
		{
			RamBankMode = true;
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
					#region 2000-3FFF
					else if (position < 0x4000)
					{
						//xxxBBBBB = Bank No.
						if (RamBankMode)
						{
							bankNum = (value & 0x1F);
						}
						else
						{
							//0xxBBBBB
							//x is other half of banknum, B is what is set here
							bankNum = (bankNum & 0x60) | (value & 0x1F);
						}
						if (bankNum == 0)
						{
							bankNum = 1;
						}
					}
					#endregion
					#region 4000-5FFF
					else if (position < 0x6000)
					{
						//Writes to 4000-5FFF will:
						//MBC1: Write RAM Bank number ******BB (4/32 RamBankMode)
						//MBC1: Write ROM upper bank num *BB***** (16/8)
						if (RamBankMode)
						{
							externalRamBank = (byte)(value & 0x03);
						}
						else
						{
							bankNum = (bankNum & 0x1F) | ((value & 0x03) << 5);
						}
					}
					#endregion
					#region 6000-7FFF
					else if (position < 0x8000 && RamEnabled)
					{
						//Writes to 6000-7FFF will:
						//MBC1: Change memory model from 16/8 to 4/32
						RamBankMode = ((value & 1) != 0);
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
