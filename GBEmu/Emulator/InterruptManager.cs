using System;

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
		private byte IE;
		private byte IF;
		private bool InterruptMasterEnable;
		public bool InterruptsReady { get { return (IE & IF) != 0; } }

		public InterruptManager()
		{
			InterruptMasterEnable = true;
		}

		private void InitializeDefaultValues()
		{
			IE = 0;
			IF = 0;
		}

		public byte Read(int position)
		{
			if (position == IntFlag) return IF;
			else if (position == IntEnable) return IE;
			return 0xFF;
		}

		public void Write(int position, byte data)
		{
			if (position == IntFlag)
			{
				IF = data;
			}
			else if (position == IntEnable)
			{
				IE = data;
			}
		}

		public void RequestInterrupt(InterruptType intType)
		{
			if (InterruptMasterEnable)
			{
				IF |= (byte)intType;
			}
		}

		public bool InterruptsEnabled()
		{
			return InterruptMasterEnable;
		}

		public void DisableInterrupts()
		{
			InterruptMasterEnable = false;
		}

		public void EnableInterrupts()
		{
			InterruptMasterEnable = true;
		}

		public InterruptType FetchNextInterrupt()
		{
			if (!InterruptMasterEnable) return InterruptType.None;
			byte triggered = (byte)(IE & IF);
			if ((triggered & 0x1) != 0)
			{
				IF ^= 0x01;
				DisableInterrupts();
				return InterruptType.VBlank;
			}
			if ((triggered & 0x2) != 0)
			{
				IF ^= 0x02;
				DisableInterrupts();
				return InterruptType.LCDC;
			}
			if ((triggered & 0x4) != 0)
			{
				IF ^= 0x04;
				DisableInterrupts();
				return InterruptType.Timer;
			}
			if ((triggered & 0x08) != 0)
			{
				IF ^= 0x08;
				DisableInterrupts();
				return InterruptType.Serial;
			}
			if ((triggered & 0x10) != 0)
			{
				IF ^= 0x10;
				DisableInterrupts();
				return InterruptType.Joypad;
			}
			return InterruptType.None;
		}
	}
}
