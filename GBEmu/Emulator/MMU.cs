using GBEmu.Emulator.Cartridge;

namespace GBEmu.Emulator
{
	public delegate void DMATransferDelegate(int address, byte data);
	class MMU : TimedIODevice
	{
		#region System Components
		private InterruptManager interruptManager;
		private TimedIODevice LCD;
		private TimedIODevice serial;
		private TimedIODevice timer;
		private IReadWriteCapable cart;
		private IReadWriteCapable input;
		private IReadWriteCapable audio;
		#endregion

		DMATransferDelegate DMATransfer;

		/// <summary>
		/// [C000-DFFF] Represents the Work RAM contained in the system.
		/// </summary>
		private byte[] internalWRAM;

		/// <summary>
		/// [FF80-FFFE] Represents the High RAM contained in the system.
		/// </summary>
		private byte[] HRAM;

		public MMU(InterruptManager iM,
			IReadWriteCapable iCart,
			IReadWriteCapable iInput,
			IReadWriteCapable iAudio,
			TimedIODevice iTimer,
			TimedIODevice iSerial,
			TimedIODevice iVideo,
			DMATransferDelegate dmaHook)
		{
			interruptManager = iM;
			cart = iCart;
			input = iInput;
			timer = iTimer;
			LCD = iVideo;
			serial = iSerial;
			audio = iAudio;
			DMATransfer = dmaHook;
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
			ushort startAddress = (ushort)(transferDetails << 8);
			ushort endAddress = 0xFE00;
			while (endAddress < 0xFEA0)
			{
				DMATransfer(endAddress++, Read(startAddress++));
			}
		}

		public override void UpdateCounter(int cycles)
		{
			if (cycles <= 0) return;
			LCD.UpdateCounter(cycles);
			timer.UpdateCounter(cycles);
			serial.UpdateCounter(cycles);
		}
	}
}
