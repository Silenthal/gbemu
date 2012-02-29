using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using GBEmu.Emulator.Cartridge;

namespace GBEmu.Emulator
{
	class MMU : TimedIODevice
	{
		public Input input;
		public GBTimer timer = new GBTimer();
		public Cart cart;
		public Video LCD;
		public Serial serial = new Serial();
		public Audio audio = new Audio();

		public bool isCGB = false;
		public bool isDoubleSpeed = false;

		public byte[] ROMFile { get { return cart.ROMFile; } }
		public bool FileLoaded { get { return cart.FileLoaded; } }

		private bool DMATransferModeEnabled = false;

		private byte[,] internalWRAM;//0xC000-0xDFFF

		private byte[] internalWRAM0;
		private byte[,] internalWRAM1;
		private int internalWRAMBankNum = 0;

		private byte[] _OAM = new byte[0xA0];

		private byte[] hardwareRegisters = new byte[0x80];

		private byte InterruptEnable;
		private byte InterruptFlag;
		private byte[] HRAM = new byte[0x7F];

		#region Memory Location Names
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
		#endregion

		public const int MODE_0_CYCLES = 204;
		public const int MODE_1_CYCLES = 1140;
		public const int MODE_2_CYCLES = 80;
		public const int MODE_3_CYCLES = 172;
		public const int LY_CYCLE = 456;
		public const int VBLANK_CYCLES = 4560;
		public const int SCREEN_DRAW_CYCLES = 70224;
		public const int LY_ONSCREEN_CYCLES = 65664;
		public const int CYCLES_PER_SECOND = 4194304;
		public const int DIV_CYCLE = 256;
		public const int DMA_CYCLE = 670;

		public byte FlagStatus
		{
			get
			{
				return (byte)(InterruptEnable & InterruptFlag);
			}
		}

		public MMU(byte[] inFile)
		{
			cart = CartLoader.LoadCart(inFile);
			input = new Input();
			LCD = new Video();
			initializeInternalRAM();
		}

		public void initializeInternalRAM()
		{
			internalWRAM = new byte[8, 0x1000];
			internalWRAM0 = new byte[0x1000];//0xC000 - CFFF
			internalWRAM1 = new byte[7, 0x1000];//D000 - DFFF
			internalWRAMBankNum = 1;
		}

		public override byte Read(int position)
		{
			if (!DMATransferModeEnabled)
			{
				if (position < 0x8000) return cart.Read(position);
				else if (position < 0xA000) return LCD.Read(position);
				else if (position < 0xC000) return cart.Read(position);
				else if (position < 0xE000) return InternalRamRead(position);
				else if (position < 0xFE00) return 0;
				else if (position < 0xFEA0) return LCD.Read(position);
				else if (position < 0xFF00) return 0;
				else if (position < 0xFF80)
				{
					switch (position & 0xFF)
					{
						case P1:
							return input.Read(position);
						case SB:
						case SC:
							return serial.Read(position);
						case DIV:
						case TIMA:
						case TMA:
						case TAC:
							return timer.Read(position);
						case IF:
							return InterruptFlag;
						case NR10:
						case NR11:
						case NR12:
						case NR13:
						case NR14:
						case NR21:
						case NR22:
						case NR23:
						case NR24:
						case NR30:
						case NR31:
						case NR32:
						case NR33:
						case NR34:
						case NR41:
						case NR42:
						case NR43:
						case NR44:
						case NR50:
						case NR51:
						case NR52:
							return audio.Read(position);
						case LCDC:
						case STAT:
						case SCX:
						case SCY:
						case LY:
						case LYC:
						case BGP:
						case OBP0:
						case OBP1:
						case WX:
						case WY:
						case VBK:
						case BCPD:
						case BCPS:
						case OCPS:
						case OCPD:
							return LCD.Read(position);
						default:
							return 0;
					}
				}
				else if (position < 0xFFFF) return HRAM[position - 0xFF80];
				else if (position == 0xFFFF) return InterruptEnable;
				else return 0;
			}
			else
			{
				if (position < 0xFFFF) return HRAM[position - 0xFF80];
				else return 0;
			}
		}

		private byte InternalRamRead(int position)
		{
			if (position >= 0xC000)
			{
				if (position < 0xD000) return internalWRAM[0, position - 0xC000];
				else if (position < 0xE000) return internalWRAM[internalWRAMBankNum, position - 0xD000];
				else return 0;
			}
			else return 0;
		}

		public override void Write(int position, byte value)
		{
			if (position < 0x8000) cart.Write(position, value);
			else if (position < 0xA000) LCD.Write(position, value);
			else if (position < 0xC000) cart.Write(position, value);
			else if (position < 0xD000) internalWRAM0[position - 0xC000] = value;
			else if (position < 0xE000) internalWRAM1[internalWRAMBankNum, position - 0xD000] = value;
			else if (position < 0xFE00) return;
			else if (position < 0xFEA0) LCD.Write(position, value);
			else if (position < 0xFF00) return;
			else if (position < 0xFF80)
			{
				switch (position & 0xFF)
				{
					case P1:
						input.Write(position, value);
						break;
					case SB:
					case SC:
						serial.Write(position, value);
						break;
					case DIV:
					case TIMA:
					case TMA:
					case TAC:
						timer.Write(position, value);
						break;
					case IF:
						InterruptFlag = value;
						break;
					case NR10:
					case NR11:
					case NR12:
					case NR13:
					case NR14:
					case NR21:
					case NR22:
					case NR23:
					case NR24:
					case NR30:
					case NR31:
					case NR32:
					case NR33:
					case NR34:
					case NR41:
					case NR42:
					case NR43:
					case NR44:
					case NR50:
					case NR51:
					case NR52:
						audio.Write(position, value);
						break;
					case LCDC:
					case STAT:
					case SCX:
					case SCY:
					case LY:
					case LYC:
					case BGP:
					case OBP0:
					case OBP1:
					case WX:
					case WY:
					case VBK:
					case BCPD:
					case BCPS:
					case OCPS:
					case OCPD:
						LCD.Write(position, value);
						if (LCD.DMATransferRequest)
						{
							DMATransferModeEnabled = true;
							DMATransferOAM(value);
						}
						break;
					default:
						break;
				}
			}
			else if (position < 0xFFFF) HRAM[position - 0xFF80] = value;
			else if (position == 0xFFFF) InterruptEnable = value;
			else return;
		}

		private void InternalRamWrite(int position, byte value)
		{
			if (position >= 0xC000)
			{
				if (position < 0xD000) internalWRAM[0, position - 0xC000] = value;
				else if (position < 0xE000) internalWRAM[internalWRAMBankNum, position - 0xD000] = value;
			}
		}

		public void DMATransferOAM(byte transferDetails)
		{
			int startAddress = transferDetails << 8;
			for (int i = 0; i < 0xA0; i++)
			{
				LCD.OAM[i + 0xFE00] = Read(startAddress + 1);
			}
		}

		public override void UpdateCounter(int cycles)
		{
			if (cycles > 0)
			{
				CycleCounter += cycles;
				LCD.UpdateCounter(cycles);
				if (!LCD.DMATransferRequest)
				{
					DMATransferModeEnabled = false;
				}
				timer.UpdateCounter(cycles);//Done
				//serial.UpdateCounter(cycles);//Not implemented...
				byte vbInt = LCD.VBlankInterrupt;
				byte lcdcint = LCD.LCDCInterrupt;
				byte serialint = serial.SerialInterrupt;
				byte timerint = timer.TimerInterrupt;
				byte joyint = input.JoyInterrupt;
				if (vbInt != 0) InterruptFlag |= vbInt;
				if (lcdcint != 0) InterruptFlag |= lcdcint;
				if (serialint != 0) InterruptFlag |= serialint;
				if (timerint != 0) InterruptFlag |= timerint;
				if (joyint != 0) InterruptFlag |= joyint;
			}
		}

		public void ResetInterruptFlag(int flagNum)
		{
			if (flagNum > 5) return;
			InterruptFlag ^= (byte)(1 << flagNum);
		}
	}
}
