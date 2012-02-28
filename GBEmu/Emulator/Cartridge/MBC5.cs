using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GBEmu.Emulator.Cartridge
{
	class MBC5 : Cart
	{
		private bool mbc5_IsRumble;

		public MBC5(byte[] inFile) : base(inFile)
		{
			romFile = new byte[inFile.Length];
			Array.Copy(inFile, romFile, inFile.Length);
			InitializeOutsideRAM();
			FileLoaded = true;
			mbc5_IsRumble = false;
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
			if (position < 0) return;
			#region 0000-1FFF
			if (position < 0x2000)
			{
				RamEnabled = ((value & 0x0F) == 0x0A);
			}
			#endregion
			#region 2000-2FFF
			else if (position < 0x3000)
			{
				bankNum = (bankNum & 0x100) | value;
			}
			#endregion
			#region 3000-3FFF
			else if (position < 0x4000)
			{
				//Writes to 2000-3FFF will:
				//MBC5: Write ROM Low Bank number (if write to 2000-2FFF) BBBBBBBB
				//MBC5: Write ROM High Bank number (if write to 3000-3FFF) *******B
				bankNum = bankNum & 0xFF | ((value & 1) << 8);
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
					externalRamBank = (byte)(value & 0x07);
					mbc5_IsRumble = (value & 0x08) == 1;
				}
				else externalRamBank = (byte)(value & 0x0F);
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
