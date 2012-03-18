namespace GBEmu.Emulator
{
	class Divider : TimedIODevice
	{
		/// <summary>
		/// Contains the cycles that occur before the divider increments.
		/// </summary>
		/// <remarks>
		/// For DMG, there are 4194304 cycles/second.
		/// The Divider ticks at 16384 Hz in GB mode.
		/// So, 256 cycles * 16384 times.
		/// </remarks>
		private const int DIV_CYCLE = 256;
		public byte DIV_Divider { get; private set; }

		public Divider()
		{
			InitializeDefaultValues();
		}

		private void InitializeDefaultValues()
		{
			DIV_Divider = 0x1C;
		}

		public override byte Read(int position)
		{
			if (position == 0xFF04)
			{
				return DIV_Divider;
			}
			return 0xFF;
		}

		public override void Write(int position, byte data)
		{
			if (position == 0xFF04)
			{
				InitializeDefaultValues();
			}
		}

		public override void UpdateCounter(int cycles)
		{
			CycleCounter += cycles;
			if (CycleCounter >= DIV_CYCLE)
			{
				CycleCounter -= DIV_CYCLE;
				DIV_Divider++;
			}
		}
	}

	class TimerCounter : TimedIODevice
	{
		private InterruptManager interruptManager;
		private const int DMG_ClockRate = 4194304;
		/// <summary>
		/// [FF05]Contains a user controllable timer that is incremented according to the Timer Control (TAC).
		/// </summary>
		/// <remarks>
		/// When the timer is active, and this overflows, it is reset to TMA.
		/// Also, when it overflows, a timer interrupt is genenrated.
		/// </remarks>
		private byte TIMA_TimerCounter;
		/// <summary>
		/// [FF06]Contains a value that is to be written to TIMA when it oveflows during an active timer.
		/// </summary>
		private byte TMA_TimerOverflowValue;
		/// <summary>
		/// [FF07]Controls the timer, as well as the rate it updates by.
		/// </summary>
		/// <remarks>
		/// Bit 2: Timer Start/Stop	(0 = off	1 = on)
		/// Bit 1-0: Timer Clock Rate
		/// -00:	4096 Hz
		/// -01:	262144 Hz
		/// -10:	65536 Hz
		/// -11:	16384 Hz
		/// </remarks>
		private byte TAC_TimerControl;//FF07
		#region TAC Related Variables
		private bool TAC_TimerEnabled { get { return (TAC_TimerControl & 0x4) != 0; } }
		private int TAC_ClockIndex { get { return TAC_TimerControl & 0x3; } }
		private int TAC_TimerCycles;
		private static int[] TAC_Timings = new int[4]
		{
			1024, //4096 Hz, updates every 1024 cycles.
			16, //262144 Hz, updates every 16 cycles.
			64, //65536 Hz, updates every 64 cycles.
			256 //16384 Hz, updates every 256 cycles.
		};
		#endregion

		public TimerCounter(InterruptManager iM)
		{
			interruptManager = iM;
		}

		private void InitializeDefaultValues()
		{
			TIMA_TimerCounter = 0;
			TMA_TimerOverflowValue = 0;
			TAC_TimerControl = 0;
			TAC_TimerCycles = 0;
		}

		public override byte Read(int position)
		{
			position &= 0xFFFF;
			if (position >= 0xFF00)
			{
				switch (position & 0xFF)
				{
					case IOPorts.TMA:
						return TMA_TimerOverflowValue;
					case IOPorts.TIMA:
						return TIMA_TimerCounter;
					case IOPorts.TAC:
						return TAC_TimerControl;
					default:
						return 0xFF;
				}
			}
			return 0xFF;
		}

		public override void Write(int position, byte data)
		{
			position &= 0xFFFF;
			if (position >= 0xFF00)
			{
				switch(position & 0xFF)
				{
					case IOPorts.TMA:
						TMA_TimerOverflowValue = data;
						break;
					case IOPorts.TIMA:
						TIMA_TimerCounter = data;
						break;
					case IOPorts.TAC:
						if (((TAC_TimerControl ^ data) & 0x04) != 0)//If timer was changed...
						{
							if ((data & 0x04) != 0)//If timer is enabled
							{
								ReinitializeTimer(TAC_Timings[data & 0x3]);
							}
							//Otherwise, do nothing.
						}
						TAC_TimerControl = (byte)(data | 0xF8);
						break;
				}
			}
		}

		private void ReinitializeTimer(int resolution)
		{
			TAC_TimerCycles = CycleCounter % resolution;
		}

		public override void UpdateCounter(int cycles)
		{
			CycleCounter += cycles;
			if (CycleCounter >= DMG_ClockRate)
			{
				CycleCounter -= DMG_ClockRate;
			}
			if (TAC_TimerEnabled)
			{
				TAC_TimerCycles += cycles;
				while (TAC_TimerCycles >= TAC_Timings[TAC_ClockIndex])
				{
					TAC_TimerCycles -= TAC_Timings[TAC_ClockIndex];
					TIMA_TimerCounter++;
					if (TIMA_TimerCounter == 0)
					{
						interruptManager.RequestInterrupt(InterruptType.Timer);
						TIMA_TimerCounter = TMA_TimerOverflowValue;
					}
				}
			}
		}
	}

	class GBTimer : TimedIODevice
	{
		private Divider divider;
		private TimerCounter timerCounter;

		public GBTimer(InterruptManager iM)
		{
			CycleCounter = 0;
			divider = new Divider();
			timerCounter = new TimerCounter(iM);
		}

		public override byte Read(int position)
		{
			switch (position & 0xFF)
			{
				case IOPorts.DIV:
					return divider.Read(position);
				case IOPorts.TIMA:
				case IOPorts.TMA:
				case IOPorts.TAC:
					return timerCounter.Read(position);
				default:
					return 0xFF;
			}
		}

		public override void Write(int position, byte data)
		{
			switch (position & 0xFF)
			{
				case IOPorts.DIV:
					divider.Write(position, data);
					break;
				case IOPorts.TIMA:
				case IOPorts.TMA:
				case IOPorts.TAC:
					timerCounter.Write(position, data);
					break;
				default:
					break;
			}
		}

		public override void UpdateCounter(int cycles)
		{
			divider.UpdateCounter(cycles);
			timerCounter.UpdateCounter(cycles);
		}
	}
}
