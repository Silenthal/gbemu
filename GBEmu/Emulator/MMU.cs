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
		#region System Components
		public Input input;
		private GBTimer timer;
		private Cart cart;
		public Video LCD;
		private Serial serial;
		private Audio audio;
		private InterruptManager interruptManager;
		#endregion

		#region System Properties
		public bool IsCGB;
		public bool IsDoubleSpeed;
		#endregion
		
		private bool DMATransferModeEnabled = false;

		private byte[,] internalWRAM;//0xC000-0xDFFF

		private byte[] internalWRAM0;
		private byte[,] internalWRAM1;
		private int internalWRAMBankNum = 0;

		private byte InterruptFlag;
		private byte[] HRAM; //FF80 - FFFE

		public const int CYCLES_PER_SECOND = 4194304;
		public const int DIV_CYCLE = 256;
		public const int DMA_CYCLE = 670;

		public MMU(byte[] inFile, InterruptManager iM, GBEmu.Render.IRenderable screen)
		{
			interruptManager = iM;
			cart = CartLoader.LoadCart(inFile);
			input = new Input(iM);
			timer = new GBTimer(iM);
			LCD = new Video(interruptManager, screen);
			serial = new Serial();
			audio = new Audio();
			initializeInternalAndHRAM();
			IsCGB = false;
			IsDoubleSpeed = false;
		}

		public void initializeInternalAndHRAM()
		{
			internalWRAM = new byte[8, 0x1000];
			internalWRAM0 = new byte[0x1000];//0xC000 - CFFF
			internalWRAM1 = new byte[7, 0x1000];//D000 - DFFF
			internalWRAMBankNum = 1;
			HRAM = new byte[0x7F];
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
				else if (position < 0x10000)
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
						case IOPorts.IE:
						case IOPorts.IF:
							return interruptManager.Read(position);
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
							if (position < 0xFFFF) return HighRamRead(position);
							return 0;
					}
				}
				else return 0;
			}
			else
			{
				return HighRamRead(position);
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

		private byte HighRamRead(int position)
		{
			if (position >= 0xFF80 && position < 0x10000)
			{
				return HRAM[position - 0xFF80];
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
			else if (position < 0x10000)
			{
				switch (position & 0xFF)
				{
					#region Writes to Input
					case IOPorts.P1:
						input.Write(position, value);
						break;
					#endregion
					#region Writes to Serial
					case IOPorts.SB:
					case IOPorts.SC:
						serial.Write(position, value);
						break;
					#endregion
					#region Writes to Timer
					case IOPorts.DIV:
					case IOPorts.TIMA:
					case IOPorts.TMA:
					case IOPorts.TAC:
						timer.Write(position, value);
						break;
					#endregion
					#region Writes to InterruptManager
					case IOPorts.IE:
					case IOPorts.IF:
						interruptManager.Write(position, value);
						break;
					#endregion
					#region Writes to Sound
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
					#endregion
					#region Writes to Video
					case IOPorts.LCDC:
					case IOPorts.STAT:
					case IOPorts.SCY:
					case IOPorts.SCX:
					case IOPorts.LY:
					case IOPorts.LYC:
					case IOPorts.DMA:
					case IOPorts.BGP:
					case IOPorts.OBP0:
					case IOPorts.OBP1:
					case IOPorts.WY:
					case IOPorts.WX:
					case IOPorts.KEY1:
					case IOPorts.VBK:
					case IOPorts.HDMA1:
					case IOPorts.HDMA2:
					case IOPorts.HDMA3:
					case IOPorts.HDMA4:
					case IOPorts.HDMA5:
					case IOPorts.RP:
					case IOPorts.BCPS:
					case IOPorts.BCPD:
					case IOPorts.OCPS:
					case IOPorts.OCPD:
						LCD.Write(position, value);
						if (LCD.DMATransferRequest)
						{
							DMATransferModeEnabled = true;
							DMATransferOAM(value);
						}
						break;
					#endregion
					default:
						HighRamWrite(position, value);
						break;
				}
			}
		}

		private void InternalRamWrite(int position, byte value)
		{
			if (position >= 0xC000)
			{
				if (position < 0xD000) internalWRAM[0, position - 0xC000] = value;
				else if (position < 0xE000) internalWRAM[internalWRAMBankNum, position - 0xD000] = value;
			}
		}

		private void HighRamWrite(int position, byte value)
		{
			if (position >= 0xFF80 && position < 0x10000)
			{
				HRAM[position - 0xFF80] = value;
			}
		}

		public void DMATransferOAM(byte transferDetails)
		{
			int startAddress = transferDetails << 8;
			for (int i = 0; i < 0xA0; i++)
			{
				LCD.OAM[i] = Read(startAddress + i);
			}
			LCD.ReconstructOAMTableDMG();
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
				byte serialint = serial.SerialInterrupt;
				if (serialint != 0) InterruptFlag |= serialint;
			}
		}
	}
}
