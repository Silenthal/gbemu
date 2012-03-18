namespace GBEmu.Emulator.Cartridge
{
	class MBC1 : Cart
	{
		private bool RamBankMode;

		public MBC1(byte[] inFile, CartFeatures cartFeatures)
			: base(inFile, cartFeatures)
		{
			RamBankMode = false;
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
			#region 2000-3FFF
			else if (position < 0x4000)
			{
				//xxxBBBBB = Bank No.
				if (RamBankMode)
				{
					RomBank = (value & 0x1F);
				}
				else
				{
					//0xxBBBBB
					//x is other half of banknum, B is what is set here
					RomBank = (RomBank & 0x60) | (value & 0x1F);
				}
				if (RomBank == 0)
				{
					RomBank = 1;
				}
			}
			#endregion
			#region 4000-5FFF
			else if (position < 0x6000)
			{
				//Write RAM Bank number ******BB (4/32, a.k.a. RamBankMode)
				//Write ROM upper bank num *BB***** (16/8)
				if (RamBankMode)
				{
					CartRamBank = (byte)(value & 0x03);
				}
				else
				{
					RomBank = (RomBank & 0x1F) | ((value & 0x03) << 5);
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
	}
}
