using System;

namespace GBEmu.Emulator
{
	[Flags]
	public enum GBKeys : byte { Up = 0x40, Down = 0x80, Left = 0x20, Right = 0x10, A = 0x1, B = 0x2, Start = 0x8, Select = 0x4 };
	class Input : IReadWriteCapable
	{
		private InterruptManager interruptManager;
		private byte LineEnabled = 0x30;
		public byte Joy1
		{
			get
			{
				byte down2 = (byte)~keyIn;
				if (IsDPadReading) down2 >>= 4;
				else if (IsBothReading) down2 = 0x0F;
				down2 &= 0x0F;
				return (byte)(LineEnabled | down2);
			}
			set
			{
				LineEnabled = (byte)(value & 0x30);
			}
		}

		private bool IsDPadReading { get { return (LineEnabled & 0x10) == 0; } }
		private bool IsButtonReading { get { return (LineEnabled & 0x20) == 0; } }
		private bool IsBothReading { get { return (LineEnabled & 0x30) == 0; } }
		private GBKeys keyIn;

		public Input(InterruptManager iM)
		{
			interruptManager = iM;
		}

		public byte Read(int position)
		{
			if (position == 0xFF00) return Joy1;
			else return 0;
		}

		public void Write(int position, byte data)
		{
			if (position == 0xFF00) Joy1 = data;
		}

		public void KeyChange(GBKeys key, bool isDown)
		{
			if (isDown)
			{
				keyIn |= key;
				interruptManager.RequestInterrupt(InterruptType.Joypad);
			}
			else
			{
				keyIn &= ~key;
			}
		}
	}
}
