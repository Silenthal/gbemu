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

		private byte[] internalWRAM;//C000-DFFF

		private byte[] HRAM; //FF80 - FFFE
		
		public MMU(byte[] inFile, InterruptManager iM, GBEmu.Render.IRenderable screen)
		{
			interruptManager = iM;
			cart = CartLoader.LoadCart(inFile);
			input = new Input(iM);
			timer = new GBTimer(iM);
			LCD = new Video(interruptManager, screen);
			serial = new Serial();
			audio = new Audio();
			InitializeInternalAndHRAM();
		}

		public void InitializeInternalAndHRAM()
		{
			internalWRAM = new byte[0x2000];
			HRAM = new byte[0x7F];
		}

		#region Reads
		public override byte Read(int position)
		{
			position &= 0xFFFF;
			if (position < 0x8000) return cart.Read(position);
			else if (position < 0xA000) return LCD.Read(position);
			else if (position < 0xC000) return cart.Read(position);
			else if (position < 0xE000) return WorkRamRead(position);
			else if (position < 0xFE00) return 0xFF;
			else if (position < 0xFEA0) return LCD.Read(position);
			else if (position < 0xFF00) return 0xFF;
			else
			{
				if (position < 0xFF80)
				{
					switch (position & 0xFF)
					{
						#region Input Read
						case IOPorts.P1:
							return input.Read(position);
						#endregion
						#region Serial Read
						case IOPorts.SB:
						case IOPorts.SC:
							return serial.Read(position);
						#endregion
						#region Timer Read
						case IOPorts.DIV:
						case IOPorts.TIMA:
						case IOPorts.TMA:
						case IOPorts.TAC:
							return timer.Read(position);
						#endregion
						#region Interrupt Manager Read
						case IOPorts.IF:
							return interruptManager.Read(position);
						#endregion
						#region Audio Read
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
						#endregion
						#region Video Read
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
							return LCD.Read(position);
						#endregion
						default:
							return 0xFF;
					}
				}
				else if (position < 0xFFFF)
				{
					return HighRamRead(position);
				}
				else
				{
					return interruptManager.Read(position);
				}
			}
		}

		private byte WorkRamRead(int position)
		{
			if (position >= 0xC000 && position < 0xE000)
			{
				return internalWRAM[position - 0xC000];
			}
			else return 0xFF;
		}

		private byte HighRamRead(int position)
		{
			if (position >= 0xFF80 && position < 0xFFFF)
			{
				return HRAM[position - 0xFF80];
			}
			else return 0xFF;
		}
		#endregion

		#region Writes
		public override void Write(int position, byte value)
		{
			position &= 0xFFFF;
			if (position < 0x8000) cart.Write(position, value);
			else if (position < 0xA000) LCD.Write(position, value);
			else if (position < 0xC000) cart.Write(position, value);
			else if (position < 0xE000) WorkRamWrite(position, value);
			else if (position < 0xFE00) return;
			else if (position < 0xFEA0) LCD.Write(position, value);
			else if (position < 0xFF00) return;
			else
			{
				if (position < 0xFF80)
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
						case IOPorts.BGP:
						case IOPorts.OBP0:
						case IOPorts.OBP1:
						case IOPorts.WY:
						case IOPorts.WX:
							LCD.Write(position, value);
							break;
						case IOPorts.DMA:
							DMATransferOAM(value);
							break;
						#endregion
						default:
							break;
					}
				}
				else if (position < 0xFFFF)
				{
					HighRamWrite(position, value);
				}
				else
				{
					interruptManager.Write(position, value);
				}
			}
		}

		private void WorkRamWrite(int position, byte value)
		{
			if (position >= 0xC000 && position < 0xE000)
			{
				internalWRAM[position - 0xC000] = value;
			}
		}

		private void HighRamWrite(int position, byte value)
		{
			if (position >= 0xFF80 && position < 0xFFFF)
			{
				HRAM[position - 0xFF80] = value;
			}
		}
		#endregion

		public void DMATransferOAM(byte transferDetails)
		{
			int startAddress = transferDetails << 8;
			for (int i = 0; i < 0xA0; i++)
			{
				Write(0xFE00 + i, Read(startAddress + i));
			}
		}

		public override void UpdateCounter(int cycles)
		{
			if (cycles > 0)
			{
				CycleCounter += cycles;
				LCD.UpdateCounter(cycles);
				timer.UpdateCounter(cycles);
				serial.UpdateCounter(cycles);
			}
		}
	}
}
