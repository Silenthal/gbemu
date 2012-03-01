using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GBEmu.Emulator
{
	[Flags]
	public enum InterruptType : byte { None = 0, VBlank = 0x1, LCDC = 0x2, Timer = 0x4, Serial = 0x8, Joypad = 0x10 }
	public enum CPUState { Normal, Halt, Stop }
	class CPUStatus : IReadWriteCapable
	{
		//Contains:
		//[FFFF]: Interrupt Enable
		//[FF0F]: Interrupt Flag
		private const int IntFlag = 0xFF0F;
		private const int IntEnable = 0xFFFF;

		public CPUState cpuState { get; set; }
		private InterruptType IE;
		private InterruptType IF;
		public bool InterruptMasterEnable { get; set; }

		public CPUStatus()
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

		public void SetInterrupt(InterruptType intType)
		{
			IF |= intType;
		}

		public void ClearAllInterrupts()
		{
			IF = InterruptType.None;
		}

		public InterruptType FetchNextInterrupt()
		{
			if ((IF & InterruptType.VBlank) != 0) return InterruptType.VBlank;
			else if ((IF & InterruptType.LCDC) != 0) return InterruptType.LCDC;
			else if ((IF & InterruptType.Timer) != 0) return InterruptType.Timer;
			else if ((IF & InterruptType.Serial) != 0) return InterruptType.Serial;
			else if ((IF & InterruptType.Joypad) != 0) return InterruptType.Joypad;
			else return InterruptType.None;
		}
	}
}
