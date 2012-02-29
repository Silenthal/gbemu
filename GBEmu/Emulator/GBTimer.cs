using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio;

namespace GBEmu.Emulator
{
    class GBTimer : TimedIODevice
    {
        private int TIMACounter;

        private byte Divider;//FF04
        private byte Timer;//FF05
        private byte TimerOverflowValue;//FF06
        private byte TimerControl;//FF07
        private bool IsTimerEnabled;
        
        private bool TimerInterruptRequest;

        private const int DIV_CYCLE = 256;
        private uint[] TIMATimings = new uint[4]
        {
            1024, //4096 Hz
            16, //262144 Hz
            64, //65536 Hz
            256 //16384 Hz
        };
        
        public byte TimerInterrupt 
        {
            get 
            {
                if (TimerInterruptRequest)
                {
                    TimerInterruptRequest = false;
                    return 0x4;
                }
                else return 0;
            }
        }

        public GBTimer()
        {
            CycleCounter = 0;
            Divider = 0;
            Timer = 0;
            TimerOverflowValue = 0;
            TIMACounter = 0;
            TimerControl = 0;
            TimerInterruptRequest = false;
        }

        public override byte Read(int position)
        {
            switch (position & 0xFF)
            {
                case MMU.DIV:
                    return Divider;
                case MMU.TIMA:
                    return Timer;
                case MMU.TMA:
                    return TimerOverflowValue;
                case MMU.TAC:
                    return TimerControl;
                default:
                    return 0;
            }
        }

        public override void UpdateCounter(int cycles)
        {
            CycleCounter += cycles;//CycleCounter will be keeping track of divider cycles
            if (CycleCounter >= DIV_CYCLE)//Increment every 256 cycles
            {
                Divider++;
                CycleCounter &= 0xFF;
            }
            if ((TimerControl & 0x4) != 0)//If timer is started...
            {
                TIMACounter += cycles;
                if (TIMACounter >= TIMATimings[TimerControl & 0x03])//Increment at rate in timings table
                {
                    if (++Timer == 0)
                    {
                        Timer = TimerOverflowValue;
                        TimerInterruptRequest = true;
                    }
                }
            }
        }

        public override void Write(int position, byte data)
        {
            switch (position & 0xFF)
            {
                case MMU.DIV:
                    Divider = 0;
                    break;
                case MMU.TIMA:
                    Timer = data;
                    break;
                case MMU.TMA:
                    TimerOverflowValue = data;
                    break;
                case MMU.TAC:
                    TimerControl = (byte)(data & 0x7);
                    if ((TimerControl & 0x4) == 0)
                    {
                        TIMACounter = 0;
                        IsTimerEnabled = false;
                    }
                    else
                    {
                        IsTimerEnabled = true;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
