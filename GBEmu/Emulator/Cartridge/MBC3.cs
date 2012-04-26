using System;

namespace GBEmu.Emulator.Cartridge
{
	class MBC3 : Cart
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
			if (LastLatchTime != null) s += temp - LastLatchTime;
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
			else return base.CartRamRead(position);
		}

		protected override void MBCWrite(int position, byte value)
		{
			#region 0000-1FFF
			if (position < 0x2000)
			{
				//xxxx0101 == on
				//xxxx0000 == off
				RamEnabled = ((value & 0x0F) == 0x0A);
			}
			#endregion
			#region 2000-3FFF
			else if (position < 0x4000)
			{
				//MBC3: Write ROM Bank number 
				//*BBBBBBB
				RomBank = value & 0x7F;
			}
			#endregion
			#region 4000-5FFF
			else if (position < 0x6000)
			{
				//Writes to 4000-5FFF will:
				//MBC3: Write RTC register number if val is >= 0x8
				//MBC3: Write RAM Bank number ******BB
				if (value < 8)
				{
					CartRamBank = (byte)(value & 0x03);
					if (TimerPresent) RTCActive = false;
				}
				if (TimerPresent && value >= 8 && value < 0xD)
				{
					RTCRegister = (byte)(value - 8);
					RTCActive = true;
				}
			}
			#endregion
			#region 6000-7FFF
			else if (position < 0x8000)
			{
				if (TimerPresent)
				{
					if (LastLatchWrite == 0 && value == 1)
					{
						LatchTime();
					}
					LastLatchWrite = value;
				}
			}
			#endregion
		}

		protected override void CartRamWrite(int position, byte value)
		{
			if (RamEnabled)
			{
				if (TimerPresent && RTCActive)
				{
					RTC[RTCRegister] = value;
				}
				else base.CartRamWrite(position, value);
			}
		}
	}
}
