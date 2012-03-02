using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GBEmu.Emulator
{
	[Flags]
	public enum InterruptType : byte { None = 0, VBlank = 0x1, LCDC = 0x2, Timer = 0x4, Serial = 0x8, Joypad = 0x10 }
	
	class InterruptManager : IReadWriteCapable
	{
		//Contains:
		//[FFFF]: Interrupt Enable
		//[FF0F]: Interrupt Flag
		private const int IntFlag = 0xFF0F;
		private const int IntEnable = 0xFFFF;

		public CPUState cpuState { get; set; }
		private InterruptType InterruptEnable;
		private InterruptType InterruptFlag;
		public bool InterruptMasterEnable { get; set; }
		public bool InterruptsReady { get { return (InterruptEnable & InterruptFlag) != InterruptType.None; } }

		public InterruptManager()
		{
			InterruptMasterEnable = true;
			InterruptEnable = InterruptType.None;
			InterruptFlag = InterruptType.None;
		}

		public byte Read(int position)
		{
			if (position == IntFlag) return (byte)InterruptFlag;
			if (position == IntEnable) return (byte)InterruptEnable;
			return 0;
		}

		public void Write(int position, byte data)
		{
			if (position == IntFlag)
			{
				InterruptFlag = (InterruptType)(data & 0x1F);
			}
			else if (position == IntEnable)
			{
				InterruptEnable = (InterruptType)(data & 0x1F);
			}
		}

		public void RequestInterrupt(InterruptType intType)
		{
			if (InterruptMasterEnable && (InterruptEnable & intType) != 0)
			{
				InterruptFlag |= intType;
			}
		}

		public void ClearAllInterrupts()
		{
			InterruptFlag = InterruptType.None;
		}

		public InterruptType FetchNextInterrupt()
		{
			if ((InterruptFlag & InterruptType.VBlank) != 0)
			{
				InterruptFlag ^= InterruptType.VBlank;
				return InterruptType.VBlank;
			}
			else if ((InterruptFlag & InterruptType.LCDC) != 0)
			{
				InterruptFlag ^= InterruptType.LCDC;
				return InterruptType.LCDC;
			}
			else if ((InterruptFlag & InterruptType.Timer) != 0)
			{
				InterruptFlag ^= InterruptType.Timer;
				return InterruptType.Timer;
			}
			else if ((InterruptFlag & InterruptType.Serial) != 0)
			{
				InterruptFlag ^= InterruptType.Serial;
				return InterruptType.Serial;
			}
			else if ((InterruptFlag & InterruptType.Joypad) != 0)
			{
				InterruptFlag ^= InterruptType.Joypad;
				return InterruptType.Joypad;
			}
			else return InterruptType.None;
		}
	}
}
