namespace GBEmu.Emulator.Timing
{
    using GBEmu.Emulator.IO;

    internal class TimerCounter : TimedIODevice
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

        private bool TAC_TimerEnabled
        {
            get
            {
                return (TAC_TimerControl & 0x4) != 0;
            }
        }

        private int TAC_ClockIndex
        {
            get
            {
                return TAC_TimerControl & 0x3;
            }
        }

        private int TAC_TimerCycles;

        private static int[] TAC_Timings = new int[4]
		{
			1024, //4096 Hz, updates every 1024 cycles.
			16, //262144 Hz, updates every 16 cycles.
			64, //65536 Hz, updates every 64 cycles.
			256 //16384 Hz, updates every 256 cycles.
		};

        #endregion TAC Related Variables

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
                switch (position & 0xFF)
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

        /// <summary>
        /// Sets the timer cycles to reflect the current time, taken off the CPU's total running time.
        /// </summary>
        /// <param name="resolution">The number of cycles to run before incrementing TIMA, as specified in the timing table.</param>
        private void ReinitializeTimer(int resolution)
        {
            TAC_TimerCycles = CycleCounter % resolution;
        }

        public override void UpdateTime(int cycles)
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
}