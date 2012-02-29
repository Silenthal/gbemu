using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GBEmu.Emulator
{
	public class IOPorts
	{
		public const byte P1 = 0x0;
		public const byte SB = 0x1;
		public const byte SC = 0x2;
		public const byte DIV = 0x4;
		public const byte TIMA = 0x5;
		public const byte TMA = 0x6;
		public const byte TAC = 0x7;
		public const byte IF = 0xF;
		public const byte NR10 = 0x10;
		public const byte NR11 = 0x11;
		public const byte NR12 = 0x12;
		public const byte NR13 = 0x13;
		public const byte NR14 = 0x14;
		public const byte NR21 = 0x16;
		public const byte NR22 = 0x17;
		public const byte NR23 = 0x18;
		public const byte NR24 = 0x19;
		public const byte NR30 = 0x1A;
		public const byte NR31 = 0x1B;
		public const byte NR32 = 0x1C;
		public const byte NR33 = 0x1D;
		public const byte NR34 = 0x1E;
		public const byte NR41 = 0x20;
		public const byte NR42 = 0x21;
		public const byte NR43 = 0x22;
		public const byte NR44 = 0x23;
		public const byte NR50 = 0x24;
		public const byte NR51 = 0x25;
		public const byte NR52 = 0x26;
		public const byte LCDC = 0x40;
		public const byte STAT = 0x41;
		public const byte SCY = 0x42;
		public const byte SCX = 0x43;
		public const byte LY = 0x44;
		public const byte LYC = 0x45;
		public const byte DMA = 0x46;
		public const byte BGP = 0x47;
		public const byte OBP0 = 0x48;
		public const byte OBP1 = 0x49;
		public const byte WY = 0x4A;
		public const byte WX = 0x4B;
		public const byte KEY1 = 0x4D;
		public const byte VBK = 0x4F;
		public const byte HDMA1 = 0x51;
		public const byte HDMA2 = 0x52;
		public const byte HDMA3 = 0x53;
		public const byte HDMA4 = 0x54;
		public const byte HDMA5 = 0x55;
		public const byte RP = 0x56;
		public const byte BCPS = 0x68;
		public const byte BCPD = 0x69;
		public const byte OCPS = 0x6A;
		public const byte OCPD = 0x6B;
		public const byte SVBK = 0x70;
	}
}
