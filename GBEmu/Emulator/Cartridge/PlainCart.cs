using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
						break;
				}
			}
			else
			{
				externalRamMap = new byte[0, 0];
			}
		}

		public override byte Read(int position)
		{
			if (position < 0x4000) return romFile[position];
			else if (position < 0x8000)
			{
				return romFile[(bankNum * 0x4000) + position - 0x4000];
			}
			else if (position > 0x9FFF && position < 0xC000 && RamEnabled)
			{
				return externalRamMap[externalRamBank, position - 0xA000];
			}
			else return 0;
		}

		public override void Write(int position, byte value)
		{
			#region 0000-1FFF
			if (position < 0x2000)
			{

			}
			#endregion
			#region 2000-2FFF
			if (position < 0x3000)
			{

			}
			#endregion
			#region 3000-3FFF
			else if (position > 0x3000 && position < 0x4000)
			{
				bankNum = value & 0x0F;
			}
			#endregion
			#region 4000-5FFF
			#endregion
			#region 6000-7FFF
			else if (position >= 0x6000 && position < 0x8000 && RamEnabled)
			{

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
