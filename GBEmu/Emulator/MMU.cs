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
						case IOPorts.P1:
							return input.Read(position);
						case IOPorts.SB:
						case IOPorts.SC:
							return serial.Read(position);
						case IOPorts.DIV:
						case IOPorts.TIMA:
						case IOPorts.TMA:
						case IOPorts.TAC:
							return timer.Read(position);
						case IOPorts.IF:
							return InterruptFlag;
						case IOPorts.NR10:
						case IOPorts.NR11:
						case IOPorts.NR12:
						case IOPorts.NR13:
						case IOPorts.NR14:
						case IOPorts.NR21:
						case IOPorts.NR22:
						case IOPorts.NR23:
						case IOPorts.NR24:
						case IOPorts.NR30:
						case IOPorts.NR31:
						case IOPorts.NR32:
						case IOPorts.NR33:
						case IOPorts.NR34:
						case IOPorts.NR41:
						case IOPorts.NR42:
						case IOPorts.NR43:
						case IOPorts.NR44:
						case IOPorts.NR50:
						case IOPorts.NR51:
						case IOPorts.NR52:
							return audio.Read(position);
						case IOPorts.LCDC:
						case IOPorts.STAT:
						case IOPorts.SCX:
						case IOPorts.SCY:
						case IOPorts.LY:
						case IOPorts.LYC:
						case IOPorts.BGP:
						case IOPorts.OBP0:
						case IOPorts.OBP1:
						case IOPorts.WX:
						case IOPorts.WY:
						case IOPorts.VBK:
						case IOPorts.BCPD:
						case IOPorts.BCPS:
						case IOPorts.OCPS:
						case IOPorts.OCPD:
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
					case IOPorts.P1:
						input.Write(position, value);
						break;
					case IOPorts.SB:
					case IOPorts.SC:
						serial.Write(position, value);
						break;
					case IOPorts.DIV:
					case IOPorts.TIMA:
					case IOPorts.TMA:
					case IOPorts.TAC:
						timer.Write(position, value);
						break;
					case IOPorts.IF:
						InterruptFlag = value;
						break;
					case IOPorts.NR10:
					case IOPorts.NR11:
					case IOPorts.NR12:
					case IOPorts.NR13:
					case IOPorts.NR14:
					case IOPorts.NR21:
					case IOPorts.NR22:
					case IOPorts.NR23:
					case IOPorts.NR24:
					case IOPorts.NR30:
					case IOPorts.NR31:
					case IOPorts.NR32:
					case IOPorts.NR33:
					case IOPorts.NR34:
					case IOPorts.NR41:
					case IOPorts.NR42:
					case IOPorts.NR43:
					case IOPorts.NR44:
					case IOPorts.NR50:
					case IOPorts.NR51:
					case IOPorts.NR52:
						audio.Write(position, value);
						break;
					case IOPorts.LCDC:
					case IOPorts.STAT:
					case IOPorts.SCX:
					case IOPorts.SCY:
					case IOPorts.LY:
					case IOPorts.LYC:
					case IOPorts.BGP:
					case IOPorts.OBP0:
					case IOPorts.OBP1:
					case IOPorts.WX:
					case IOPorts.WY:
					case IOPorts.VBK:
					case IOPorts.BCPD:
					case IOPorts.BCPS:
					case IOPorts.OCPS:
					case IOPorts.OCPD:
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
