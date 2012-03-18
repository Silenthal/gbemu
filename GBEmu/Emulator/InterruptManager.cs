using System;

namespace GBEmu.Emulator
{
	[Flags]
	public enum InterruptType : byte { None = 0, VBlank = 0x1, LCDC = 0x2, Timer = 0x4, Serial = 0x8, Joypad = 0x10 }
	
	class InterruptManager : IReadWriteCapable
	{
		private const int IntFlag = 0xFF0F;
		private const int IntEnable = 0xFFFF;

		/// <summary>
		/// [FFFF]Enables/disables the handling of the interrupt flags.
		/// </summary>
		/// <remarks>
		/// Bit 0: V-Blank Interrupt Enable
		/// Bit 1: LCDC Interrupt Enable
		/// Bit 2: Timer Interrupt Enable
		/// Bit 3: Serial Interrupt Enable
		/// Bit 4: Joypad Interrupt Enable
		/// </remarks>
		private byte IE;
		/// <summary>
		/// [FF0F]Contains interrupts flagged by the various systems in the Game Boy.
		/// </summary>
		/// <remarks>
		/// Bit 0: V-Blank Flag
		/// Bit 1: LCDC Flag
		/// Bit 2: Timer Flag
		/// Bit 3: Serial Flag
		/// Bit 4: Joypad Flag
		/// </remarks>
		private byte IF;
		/// <summary>
		/// The master flag for interrupts. Set to false to prevent interrupts from ocurring, even when requested.
		/// </summary>
		public bool InterruptMasterEnable { get; private set; }
		/// <summary>
		/// Returns true whether interrupts are ready and waiting to be handled.
		/// </summary>
		public bool InterruptsReady { get { return (IE & IF) != 0; } }

		public InterruptManager()
		{
			InterruptMasterEnable = true;
			InitializeDefaultValues();
		}

		private void InitializeDefaultValues()
		{
			IE = 0;
			IF = 0xE0;
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
				IF = (byte)(data | 0xE0);
			}
			else if (position == IntEnable)
			{
				IE = data;
			}
		}

		/// <summary>
		/// Request that an interrupt be flagged in [IF].
		/// </summary>
		/// <param name="intType">The type of interrupt to flag.</param>
		public void RequestInterrupt(InterruptType intType)
		{
			IF |= (byte)intType;
		}

		/// <summary>
		/// Disables interrupts.
		/// </summary>
		public void DisableInterrupts()
		{
			InterruptMasterEnable = false;
		}

		/// <summary>
		/// Enables interrupts.
		/// </summary>
		public void EnableInterrupts()
		{
			InterruptMasterEnable = true;
		}

		/// <summary>
		/// Returns the next unhandled interrupt. If there is more than one flagged and enabled, the one with the highest priority is returned.
		/// </summary>
		/// <remarks>
		/// Interrupts are triggered in HALT state even if IME is off.
		/// When an interrupt ends the HALT state, the flag isn't cleared afterwards.
		/// </remarks>
		/// <returns>The type of interrupt being handled.</returns>
		public InterruptType FetchNextInterrupt(CPUState currentState)
		{
			if (!InterruptMasterEnable && currentState != CPUState.Halt) return InterruptType.None;
			byte triggered = (byte)(IE & IF);
			InterruptType returned = InterruptType.None;
			#region Getting interrupt according to priority
			if ((triggered & 0x01) != 0)
			{
				returned = InterruptType.VBlank;
			}
			if ((triggered & 0x02) != 0)
			{
				returned = InterruptType.LCDC;
			}
			if ((triggered & 0x04) != 0)
			{
				returned = InterruptType.Timer;
			}
			if ((triggered & 0x08) != 0)
			{
				return InterruptType.Serial;
			}
			if ((triggered & 0x10) != 0)
			{
				returned = InterruptType.Joypad;
			}
			#endregion
			if (returned == InterruptType.None) return returned;
			if (currentState != CPUState.Halt)
			{
				IF ^= (byte)returned;
				DisableInterrupts();
			}
			return returned;
		}
	}
}
