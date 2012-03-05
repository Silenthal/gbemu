namespace GBEmu.Emulator
{
	class GBTimer : TimedIODevice
	{
		private InterruptManager interruptManager;
		private int TIMACounter;

		private byte DIV_Divider;//FF04
		private byte TIMA_Timer;//FF05
		private byte TMA_TimerOverflowValue;//FF06
		private byte TAC_TimerControl;//FF07
		private bool TimerEnabled { get { return (TAC_TimerControl & TAC_TimerStatus) != 0; } }
		private int TAC_ClockIndex { get { return TAC_TimerControl & TAC_InputClock; } }

		private const int DIV_CYCLE = 256;
		private const int TAC_TimerStatus = 0x4;
		private const int TAC_InputClock = 0x3;
		private uint[] TIMATimings = new uint[4]
		{
			1024, //4096 Hz
			16, //262144 Hz
			64, //65536 Hz
			256 //16384 Hz
		};

		public GBTimer(InterruptManager iM)
		{
			interruptManager = iM;
			CycleCounter = 0;
			TIMACounter = 0;
			InitializeDefaultValues();
		}

		public void InitializeDefaultValues()
		{
			DIV_Divider = 0;
			TIMA_Timer = 0;
			TMA_TimerOverflowValue = 0;
			TAC_TimerControl = 0;
		}

		public override byte Read(int position)
		{
			switch (position & 0xFF)
			{
				case IOPorts.DIV:
					return DIV_Divider;
				case IOPorts.TIMA:
					return TIMA_Timer;
				case IOPorts.TMA:
					return TMA_TimerOverflowValue;
				case IOPorts.TAC:
					return TAC_TimerControl;
				default:
					return 0;
			}
		}

		public override void Write(int position, byte data)
		{
			switch (position & 0xFF)
			{
				case IOPorts.DIV:
					DIV_Divider = 0;
					break;
				case IOPorts.TIMA:
					TIMA_Timer = data;
					break;
				case IOPorts.TMA:
					TMA_TimerOverflowValue = data;
					break;
				case IOPorts.TAC:
					if ((data & TAC_TimerStatus) == 1 && !TimerEnabled)
					{
						TIMACounter = 0;
					}
					TAC_TimerControl = (byte)(data | 0xF8);
					break;
				default:
					break;
			}
		}

		public override void UpdateCounter(int cycles)
		{
			CycleCounter += cycles;//CycleCounter will be keeping track of divider cycles
			if (CycleCounter >= DIV_CYCLE)//Increment every 256 cycles
			{
				DIV_Divider++;
				CycleCounter &= 0xFF;
			}
			if (TimerEnabled)//If timer is started...
			{
				TIMACounter += cycles;
				if (TIMACounter >= TIMATimings[TAC_ClockIndex])//Increment at rate in timings table
				{
					TIMA_Timer++;
					if (TIMA_Timer == 0)
					{
						TIMA_Timer = TMA_TimerOverflowValue;
						interruptManager.RequestInterrupt(InterruptType.Timer);
					}
				}
			}
		}
	}
}
