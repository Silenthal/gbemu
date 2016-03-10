namespace GBEmu.Emulator.Cartridge
{
    using System;
    using GBEmu.Emulator.Debug;
    using GBEmu.Emulator.Timing;

    internal class MBC3 : Cart
    {
        private bool RTCActive;
        private byte[] RTC;
        private DateTime LastLatchTime;
        private byte RTCRegister;
        private byte LastLatchWrite;

        public MBC3(byte[] inFile, CartFeatures cartFeatures)
            : base(inFile, cartFeatures)
        {
            RTC = new byte[5];
            RTCRegister = 0;
            LastLatchWrite = 0xFF;
            RTCActive = false;
        }

        public void LatchTime()
        {
            //RTC registers are like so:
            //[0] = Seconds
            //[1] = Minutes
            //[2] = Hours
            //[3] = Days (lower 8)
            //[4] = Days (upper 1)
            //If value written to activate is 1, and 0 was written before, latch rtc.
            //If clock was halted, then latch the last time. Else, latch a
            //new time where the rtc regs will equal the time last accessed,
            //plus the time difference between then and now.

            //If Stop Timer (6th bit of DH) is set, return with the old time in rtc.
            if ((RTC[4] & 0x40) == 1)
            {
                return;
            }

            //Else, latch new time.
            TimeSpan s = new TimeSpan(((RTC[4] & 1) << 9) | RTC[3], RTC[2], RTC[1], RTC[0]);
            DateTime temp = DateTime.Now;
            if (LastLatchTime != null)
                s += temp - LastLatchTime;
            LastLatchTime = temp;
            bool over = false;
            if (s > TimeSpan.FromDays(511))
            {
                //Difference is greater than 511, so oveflow is present.
                while (s.Days > 511)
                {
                    s -= TimeSpan.FromDays(511);
                }
                over = true;
            }

            //Write new times.
            RTC[0] = (byte)s.Seconds;
            RTC[1] = (byte)s.Minutes;
            RTC[2] = (byte)s.Hours;
            RTC[3] = (byte)s.Days;
            RTC[4] = (byte)((RTC[4] & 0xFE) | (s.Days >> 8));
            if (over)
            {
                //Overflow will set the last bit of DH
                RTC[4] |= 0x80;
            }
        }

        protected override byte CartRamRead(int position)
        {
            if (RamEnabled && TimerPresent && RTCActive)
            {
                return RTC[RTCRegister];
            }
            else
                return base.CartRamRead(position);
        }

        protected override void MBCWrite(int position, byte value)
        {
            switch (position >> 13)
            {
                case 0://0x0000 - 0x1FFF
                    RamEnabled = (value & 0xF) == 0xA;
                    break;

                case 1://0x2000-0x3FFF
                    RomBank = value & 0x7F;
                    break;

                case 2://0x4000-5FFF
                    if (value < 8)
                    {
                        CartRamBank = value & 0x03;
                        if (TimerPresent)
                        {
                            RTCActive = false;
                        }
                    }
                    else if (TimerPresent)
                    {
                        if (value < 0xD)
                        {
                            RTCRegister = (byte)(value & 7);
                            RTCActive = true;
                        }
                        else
                            RTCActive = false;
                    }
                    break;

                case 3://0x6000-0x7FFF
                    if (TimerPresent)
                    {
                        if (LastLatchWrite == 0 && value == 1)
                        {
                            LatchTime();
                        }
                        LastLatchWrite = value;
                    }
                    break;
            }
        }

        protected override void CartRamWrite(int position, byte value)
        {
            if (RamEnabled)
            {
                if (TimerPresent && RTCActive)
                {
                    RTC[RTCRegister] = value;
                }
                else
                    base.CartRamWrite(position, value);
            }
            else
            {
                Logger.GetInstance().Log(new LogMessage() {
                    source = LogMessageSource.Cart, position = position.ToString("X4"), time = GlobalTimer.GetInstance().GetTime(), message = "Write during RAM disable."
                });
                return;
            }
        }
    }
}