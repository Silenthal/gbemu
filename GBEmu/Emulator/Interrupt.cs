using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GBEmu.Emulator
{
	[Flags]
	public enum InterruptType : byte { VBLANK = 0x1, LCDC = 0x2, TIMER = 0x4, SERIAL = 0x8, JOYPAD = 0x10 }
	class Interrupt : IReadWriteCapable
	{
		//Contains:
		//[FFFF]: Interrupt Enable
		//[FF0F]: Interrupt Flag
		private const int IntFlag = 0xFF0F;
		private const int IntEnable = 0xFFFF;

		private InterruptType IE;
		private InterruptType IF;
		public bool InterruptMasterEnable { get; set; }

		public Interrupt()
		{
			InterruptMasterEnable = true;
		}

		public byte Read(int position)
		{
			if (position == IntFlag) return (byte)IF;
			if (position == IntEnable) return (byte)IE;
			return 0;
		}

		public void Write(int position, byte data)
		{
			if (position == IntFlag)
			{
				IF = (InterruptType)(data & 0x1F);
			}
			else if (position == IntEnable)
			{
				IE = (InterruptType)(data & 0x1F);
			}
		}
	}
}
