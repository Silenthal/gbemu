using System;

namespace GBEmu.Emulator.Cartridge
{
	class MBC3 : Cart
	{
		private bool IsLatchOn = false;
		private byte[] rtc = new byte[5];
		private DateTime LastLatchTime;
		private byte[] lastRtc = new byte[5];
		private byte rtcReg = 0;

		public MBC3(byte[] inFile) : base(inFile)
		{

		}

		public void ClockTick()
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
			if ((rtc[4] & 0x40) == 1)
			{
				return;
			}
			//Else, latch new time.
			TimeSpan s = new TimeSpan(((rtc[4] & 1) << 9) | rtc[3], rtc[2], rtc[1], rtc[0]);
			DateTime temp = DateTime.Now;
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
			rtc[0] = (byte)s.Seconds;
			rtc[1] = (byte)s.Minutes;
			rtc[2] = (byte)s.Hours;
			rtc[3] = (byte)s.Days;
			rtc[4] = (byte)((rtc[4] & 0xFE) | (s.Days >> 8));
			if (over)
			{
				//Overflow will set the last bit of DH
				rtc[4] |= 0x80;
			}
		}

		protected override void InitializeOutsideRAM()
		{
			if (RamPresent)
			{
				switch (romFile[0x149])
				{
					case 0x01:
						externalRamMap = new byte[1, 0x800];//A000-A7FF, 2 kilobytes
						break;
					case 0x02:
						externalRamMap = new byte[1, 0x2000];//A000-BFFF, 8 kilobytes
						break;
					case 0x03:
						externalRamMap = new byte[4, 0x2000];//A000-BFFF x 4, 32 kilobytes
						break;
					default:
						externalRamMap = new byte[0, 0];
						break;
				}
			}
			else
			{
				externalRamMap = new byte[0, 0];
			}
		}

		public override byte Read(int position)
		{
			if (position < 0x4000) return romFile[position];
			else if (position < 0x8000)
			{
				return romFile[(bankNum * 0x4000) + position - 0x4000];
			}
			else if (position > 0x9FFF && position < 0xC000 && RamEnabled)
			{
				return externalRamMap[externalRamBank, position - 0xA000];
			}
			else return 0;
		}

		public override void Write(int position, byte value)
		{
			#region 0000-1FFF
			if (position < 0x2000)
			{
				RamEnabled = ((value & 0x0F) == 0x0A);
			}
			#endregion
			#region 3000-3FFF
			else if (position >= 0x3000 && position < 0x4000)
			{
				//Writes to 2000-3FFF will:
				//MBC3: Write ROM Bank number *BBBBBBB
				bankNum = value & 0x7F;
			}
			#endregion
			#region 4000-5FFF
			else if (position < 0x6000)
			{
				//Writes to 4000-5FFF will:
				//MBC3: Write RTC register number if val is >= 0x8
				//MBC3: Write RAM Bank number ******BB
				if (TimerPresent && ((value & 8) != 0) && value <= 0xD)
				{
					rtcReg = (byte)(value & 0x7);
				}
				else externalRamBank = (byte)(value & 0x03);
			}
			#endregion
			#region A000-BFFF
			else if (position > 0x9FFF & position < 0xC000 & RamEnabled)
			{
				if (TimerPresent && IsLatchOn)
				{
					rtc[rtcReg] = value;
				}
				else externalRamMap[externalRamBank, position - 0xA000] = value;
			}
			#endregion
		}
	}
}
