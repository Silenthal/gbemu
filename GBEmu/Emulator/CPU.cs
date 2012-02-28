﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace GBEmu.Emulator
{
	[StructLayout(LayoutKind.Explicit)]
	public struct Register
	{
		[FieldOffset(0)]
		public ushort w;
		[FieldOffset(0)]
		public byte lo;
		[FieldOffset(1)]
		public byte hi;
	}

	class CPU
	{
		public bool FrameDone = false;
		private bool InterruptMasterEnable = true;
		private byte A = 0;
		private bool RepeatLastInstruction = false;
		private ushort[] interruptVectors = new ushort[5]
		{
			0x40, 
			0x48, 
			0x50, 
			0x58, 
			0x60
		};
		private bool IsZero
		{
			get
			{
				return (AF.w & FLAG_ZERO_ON) != 0;
			}
			set
			{
				if (value)
				{
					AF.lo |= FLAG_ZERO_ON;
				}
				else
				{
					AF.lo &= FLAG_ZERO_OFF;
				}
			}
		}
		private bool IsHalfCarry
		{
			get
			{
				return (AF.w & FLAG_HALF_CARRY_ON) != 0;
			}
			set
			{
				if (value)
				{
					AF.lo |= FLAG_HALF_CARRY_ON;
				}
				else
				{
					AF.lo &= FLAG_HALF_CARRY_OFF;
				}
			}
		}
		private bool IsCarry
		{
			get
			{
				return (AF.w & FLAG_CARRY_ON) != 0;
			}
			set
			{
				if (value)
				{
					AF.lo |= FLAG_CARRY_ON;
				}
				else
				{
					AF.lo &= FLAG_CARRY_OFF;
				}
			}
		}
		private bool IsNegativeOp
		{
			get
			{
				return (AF.w & FLAG_NEGATIVE_ON) != 0;
			}
			set
			{
				if (value)
				{
					AF.lo |= FLAG_NEGATIVE_ON;
				}
				else
				{
					AF.lo &= FLAG_NEGATIVE_OFF;
				}
			}
		}
		public Register AF;
		public Register BC;
		public Register DE;
		public Register HL;
		public Register PC;
		public Register SP;

		public MMU mmu;

		public int CycleCounter { get; private set; }

		private enum Reg : byte { REG_A, REG_B, REG_C, REG_D, REG_E, REG_H, REG_L, REG_AF, REG_BC, REG_DE, REG_HL, REG_SP }

		#region Flag Constants
		#region Enable Constants (OR)
		private const byte FLAG_ZERO_ON = 0x80;
		private const byte FLAG_NEGATIVE_ON = 0x40;
		private const byte FLAG_HALF_CARRY_ON = 0x20;
		private const byte FLAG_CARRY_ON = 0x10;
		#endregion

		#region Disable Constants (AND)
		private const byte FLAG_ZERO_OFF = 0x7F;
		private const byte FLAG_NEGATIVE_OFF = 0xBF;
		private const byte FLAG_HALF_CARRY_OFF = 0xDF;
		private const byte FLAG_CARRY_OFF = 0xEF;
		#endregion

		#region Set
		private const byte SET_7 = 0x80;
		private const byte SET_6 = 0x40;
		private const byte SET_5 = 0x20;
		private const byte SET_4 = 0x10;
		private const byte SET_3 = 0x08;
		private const byte SET_2 = 0x04;
		private const byte SET_1 = 0x02;
		private const byte SET_0 = 0x01;
		#endregion

		#region Reset
		private const byte RESET_7 = 0x7F;
		private const byte RESET_6 = 0xBF;
		private const byte RESET_5 = 0xDF;
		private const byte RESET_4 = 0xEF;
		private const byte RESET_3 = 0xF7;
		private const byte RESET_2 = 0xFB;
		private const byte RESET_1 = 0xFD;
		private const byte RESET_0 = 0xFE;
		#endregion
		#endregion

		#region Two's Table
		public static int[] twosTable = new int[256]
		{
			0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 
			16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 
			32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 
			48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 
			64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 
			80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 
			96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 
			112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, -128, 
			-127, -126, -125, -124, -123, -122, -121, -120, -119, -118, -117, -116, -115, -114, -113, -112, 
			-111, -110, -109, -108, -107, -106, -105, -104, -103, -102, -101, -100, -99, -98, -97, -96, 
			-95, -94, -93, -92, -91, -90, -89, -88, -87, -86, -85, -84, -83, -82, -81, -80, 
			-79, -78, -77, -76, -75, -74, -73, -72, -71, -70, -69, -68, -67, -66, -65, -64, 
			-63, -62, -61, -60, -59, -58, -57, -56, -55, -54, -53, -52, -51, -50, -49, -48, 
			-47, -46, -45, -44, -43, -42, -41, -40, -39, -38, -37, -36, -35, -34, -33, -32, 
			-31, -30, -29, -28, -27, -26, -25, -24, -23, -22, -21, -20, -19, -18, -17, -16, 
			-15, -14, -13, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1
		};
		#endregion

		public bool IsStopped { get; private set; }
		public bool IsHalted { get; private set; }
		public CPU(byte[] inFile)
		{
			mmu = new MMU(inFile);
			//GB defaults...
			PC.w = 0x0100;
			SP.w = 0xFFFE;
			AF.w = 0x01B0;
			BC.w = 0x0013;
			DE.w = 0x00D8;
			HL.w = 0x014D;
		}

		public void step(int cycles)
		{
			while (cycles > 0)
			{
				//Halt increments the PC, waiting for an interrupt
				//Stop doesn't increment the PC, and turns off the LCD.
				//Also, speed switch occurs after stop is used.
				CycleCounter = 0;
				bool interruptHandled = false;
				byte fs = mmu.FlagStatus;
				if (InterruptMasterEnable && fs != 0) interruptHandled = CheckInterrupts(fs);
				if (IsHalted)//Halt increments PC by 4
				{
					if (interruptHandled) IsHalted = false;
					else CycleCounter += 4;
				}
				else if (IsStopped)
				{
					if (interruptHandled) IsStopped = false;
				}
				else
				{
					byte inst = ReadPC();
					if (RepeatLastInstruction)//Halt bug in non-CGB systems.
					{
						PC.w--;
						RepeatLastInstruction = false;
					}
					switch (inst)
					{
						#region Ops 0x00-0x0F (checked 2011/11/10)
						case 0x0://nop
							break;
						case 0x1://ld bc,nnnn
							Load16Immediate(ref BC);
							break;
						case 0x2://ld [bc],a
							Write(BC.w, A);
							break;
						case 0x3://inc bc
							Inc16(ref BC.w);
							break;
						case 0x4://inc b
							Inc(ref BC.hi);
							break;
						case 0x5://dec b
							Dec(ref BC.hi);
							break;
						case 0x6://ld b,nn
							BC.hi = ReadPC();
							break;
						case 0x7://rlca
							RLC(ref A);
							break;
						case 0x8://ld [nnnn],sp
							Register address = new Register();
							address.lo = ReadPC();
							address.hi = ReadPC();
							Write(address.w++, SP.lo);
							Write(address.w, SP.hi);
							break;
						case 0x9://add hl,bc
							AddHL(BC.w);
							break;
						case 0xA://ld a,[bc]
							A = Read(BC.w);
							break;
						case 0xB://dec bc
							Dec16(ref BC.w);
							break;
						case 0xC://inc c
							Inc(ref BC.lo);
							break;
						case 0xD://dec c
							Dec(ref BC.lo);
							break;
						case 0xE://ld c,nn
							BC.lo = ReadPC();
							break;
						case 0xF://rrca
							RRC(ref A);
							break;
						#endregion
						#region Ops 0x10-0x1F (checked 2011/11/10)
						case 0x10://stop
							//Stop needs to turn off the LCD...
							IsStopped = true;
							PC.w++;
							break;
						case 0x11://ld de,nnnn
							Load16Immediate(ref DE);
							break;
						case 0x12://ld [de],a
							Write(DE.w, A);
							break;
						case 0x13://inc de
							Inc16(ref DE.w);
							break;
						case 0x14://inc d
							Inc(ref DE.hi);
							break;
						case 0x15://dec d
							Dec(ref DE.hi);
							break;
						case 0x16://ld d,nn
							DE.hi = ReadPC();
							break;
						case 0x17://rla
							RL(ref A);
							break;
						case 0x18://jr nn
							JumpRelative(true);
							break;
						case 0x19://add hl,de
							AddHL(DE.w);
							break;
						case 0x1A://ld a,[de]
							A = Read(DE.w);
							break;
						case 0x1B://dec de
							Dec16(ref DE.w);
							break;
						case 0x1C://inc e
							Inc(ref DE.lo);
							break;
						case 0x1D://dec e
							Dec(ref DE.lo);
							break;
						case 0x1E://ld e,nn
							DE.lo = ReadPC();
							break;
						case 0x1F://rra
							RR(ref A);
							break;
						#endregion
						#region Ops 0x20-0x2F (checked 2011/11/10)
						case 0x20://jr nz,nn
							JumpRelative(!IsZero);
							break;
						case 0x21://ld hl,nnnn
							Load16Immediate(ref HL);
							break;
						case 0x22://ldi [hl],a
							Write(HL.w++, A);
							break;
						case 0x23://inc hl
							Inc16(ref HL.w);
							break;
						case 0x24://inc h
							Inc(ref HL.hi);
							break;
						case 0x25://dec h
							Dec(ref HL.hi);
							break;
						case 0x26://ld h,nn
							HL.hi = ReadPC();
							break;
						case 0x27://daa
							IsZero = A == 0;
							int correct = IsCarry ? 0x60 : 0x00;
							if (IsHalfCarry) correct |= 0x06;
							if (!(IsNegativeOp))
							{
								if ((A & 0x0F) > 0x09) correct |= 0x06;
								if (A > 0x99) correct |= 0x60;
								A += (byte)correct;
							}
							else
							{
								A -= (byte)correct;
							}
							IsCarry = ((correct << 2) & 0x100) != 0;
							break;
						case 0x28://jr z,nn
							JumpRelative(IsZero);
							break;
						case 0x29://add hl,hl
							AddHL(HL.w);
							break;
						case 0x2A://ldi a,[hl]
							A = Read(HL.w++);
							break;
						case 0x2B://dec hl
							Dec16(ref HL.w);
							break;
						case 0x2C://inc l
							Inc(ref HL.lo);
							break;
						case 0x2D://dec l
							Dec(ref HL.lo);
							break;
						case 0x2E://ld l,nn
							HL.lo = ReadPC();
							break;
						case 0x2F://cpl
							IsNegativeOp = true;
							IsHalfCarry = true;
							A ^= 0xFF;
							break;
						#endregion
						#region Ops 0x30-0x3F (checked 2011/11/10)
						case 0x30://jr nc,nn
							JumpRelative(!IsCarry);
							break;
						case 0x31://ld sp,nnnn
							Load16Immediate(ref SP);
							break;
						case 0x32://ldd [hl],a
							Write(HL.w--, A);
							break;
						case 0x33://inc sp
							Inc16(ref SP.w);
							break;
						case 0x34://inc [hl]
							byte incHL = Read(HL.w);
							incHL++;
							Write(HL.w, incHL);
							break;
						case 0x35://dec [hl]
							byte decHL = Read(HL.w);
							decHL--;
							Write(HL.w, decHL);
							break;
						case 0x36://ld [hl],nn
							Write(HL.w, ReadPC());
							break;
						case 0x37://scf
							IsCarry = true;
							IsHalfCarry = false;
							IsNegativeOp = false;
							break;
						case 0x38://jr c,nn
							JumpRelative(IsCarry);
							break;
						case 0x39://add hl,sp
							AddHL(SP.w);
							break;
						case 0x3A://ldd a,[hl]
							A = Read(HL.w--);
							break;
						case 0x3B://dec sp
							Dec16(ref SP.w);
							break;
						case 0x3C://inc a
							Inc(ref A);
							break;
						case 0x3D://dec a
							Dec(ref A);
							break;
						case 0x3E://ld a,nn
							A = ReadPC();
							break;
						case 0x3F://ccf
							IsCarry = !IsCarry;
							IsHalfCarry = false;
							IsNegativeOp = false;
							break;
						#endregion
						#region Ops 0x40-0x4F (checked 2011/11/10)
						case 0x40://ld b,b
							break;
						case 0x41://ld b,c
							BC.hi = BC.lo;
							break;
						case 0x42://ld b,d
							BC.hi = DE.hi;
							break;
						case 0x43://ld b,e
							BC.hi = DE.lo;
							break;
						case 0x44://ld b,h
							BC.hi = HL.hi;
							break;
						case 0x45://ld b,l
							BC.hi = HL.lo;
							break;
						case 0x46://ld b,[hl]
							BC.hi = Read(HL.w);
							break;
						case 0x47://ld b,a
							BC.hi = A;
							break;
						case 0x48://ld c,b
							BC.lo = BC.hi;
							break;
						case 0x49://ld c,c
							break;
						case 0x4A://ld c,d
							BC.lo = DE.hi;
							break;
						case 0x4B://ld c,e
							BC.lo = DE.lo;
							break;
						case 0x4C://ld c,h
							BC.lo = HL.hi;
							break;
						case 0x4D://ld c,l
							BC.lo = HL.lo;
							break;
						case 0x4E://ld c,[hl]
							BC.lo = Read(HL.w);
							break;
						case 0x4F://ld c,a
							BC.lo = A;
							break;
						#endregion
						#region Ops 0x50-0x5F (checked 2011/11/10)
						case 0x50://ld d,b
							DE.hi = BC.hi;
							break;
						case 0x51://ld d,c
							DE.hi = BC.lo;
							break;
						case 0x52://ld d,d
							break;
						case 0x53://ld d,e
							DE.hi = DE.lo;
							break;
						case 0x54://ld d,h
							DE.hi = HL.hi;
							break;
						case 0x55://ld d,l
							DE.hi = HL.lo;
							break;
						case 0x56://ld d,[hl]
							DE.hi = Read(HL.w);
							break;
						case 0x57://ld d,a
							DE.hi = A;
							break;
						case 0x58://ld e,b
							DE.lo = BC.hi;
							break;
						case 0x59://ld e,c
							DE.lo = BC.lo;
							break;
						case 0x5A://ld e,d
							DE.lo = DE.hi;
							break;
						case 0x5B://ld e,e
							break;
						case 0x5C://ld e,h
							DE.lo = HL.hi;
							break;
						case 0x5D://ld e,l
							DE.lo = HL.lo;
							break;
						case 0x5E://ld e,[hl]
							DE.lo = Read(HL.w);
							break;
						case 0x5F://ld e,a
							DE.lo = A;
							break;
						#endregion
						#region Ops 0x60-0x6F (checked 2011/11/10)
						case 0x60://ld h,b
							HL.hi = BC.hi;
							break;
						case 0x61://ld h,c
							HL.hi = BC.lo;
							break;
						case 0x62://ld h,d
							HL.hi = DE.hi;
							break;
						case 0x63://ld h,e
							HL.hi = DE.lo;
							break;
						case 0x64://ld h,h
							break;
						case 0x65://ld h,l
							HL.hi = HL.lo;
							break;
						case 0x66://ld h,[hl]
							HL.hi = Read(HL.w);
							break;
						case 0x67://ld h,a
							HL.hi = A;
							break;
						case 0x68://ld l,b
							HL.lo = BC.hi;
							break;
						case 0x69://ld l,c
							HL.lo = BC.lo;
							break;
						case 0x6A://ld l,d
							HL.lo = DE.hi;
							break;
						case 0x6B://ld l,e
							HL.lo = DE.lo;
							break;
						case 0x6C://ld l,h
							HL.lo = HL.hi;
							break;
						case 0x6D://ld l,l
							break;
						case 0x6E://ld l,[hl]
							HL.lo = Read(HL.w);
							break;
						case 0x6F://ld l,a
							HL.lo = A;
							break;
						#endregion
						#region Ops 0x70-0x7F (checked 2011/11/10)
						case 0x70://ld [hl],b
							Write(HL.w, BC.hi);
							break;
						case 0x71://ld [hl],c
							Write(HL.w, BC.lo);
							break;
						case 0x72://ld [hl],d
							Write(HL.w, DE.hi);
							break;
						case 0x73://ld [hl],e
							Write(HL.w, DE.lo);
							break;
						case 0x74://ld [hl],h
							Write(HL.w, HL.hi);
							break;
						case 0x75://ld [hl],l
							Write(HL.w, HL.lo);
							break;
						case 0x76://halt
							if (InterruptMasterEnable) IsHalted = true;
							else if (fs != 0)
							{
								//Handle GBC mode
								if (mmu.isCGB) CycleCounter += 4;
								else RepeatLastInstruction = true;
							}
							break;
						case 0x77://ld [hl],a
							Write(HL.w, A);
							break;
						case 0x78://ld a,b
							A = BC.hi;
							break;
						case 0x79://ld a,c
							A = BC.lo;
							break;
						case 0x7A://ld a,d
							A = DE.hi;
							break;
						case 0x7B://ld a,e
							A = DE.lo;
							break;
						case 0x7C://ld a,h
							A = HL.hi;
							break;
						case 0x7D://ld a,l
							A = HL.lo;
							break;
						case 0x7E://ld a,hl
							A = Read(HL.w);
							break;
						case 0x7F://ld a,a
							break;
						#endregion
						#region Ops 0x80-0x8F (checked 2011/11/10)
						case 0x80://add a,b
							AddA(BC.hi, false);
							break;
						case 0x81://add a,c
							AddA(BC.lo, false);
							break;
						case 0x82://add a,d
							AddA(DE.hi, false);
							break;
						case 0x83://add a,e
							AddA(DE.lo, false);
							break;
						case 0x84://add a,h
							AddA(HL.hi, false);
							break;
						case 0x85://add a,l
							AddA(HL.lo, false);
							break;
						case 0x86://add a,[hl]
							AddA(Read(HL.w), false);
							break;
						case 0x87://add a,a
							AddA(A, false);
							break;
						case 0x88://adc a,b
							AddA(BC.hi, true);
							break;
						case 0x89://adc a,c
							AddA(BC.lo, true);
							break;
						case 0x8A://adc a,d
							AddA(DE.hi, true);
							break;
						case 0x8B://adc a,e
							AddA(DE.lo, true);
							break;
						case 0x8C://adc a,h
							AddA(HL.hi, true);
							break;
						case 0x8D://adc a,l
							AddA(HL.lo, true);
							break;
						case 0x8E://adc a,[hl]
							AddA(Read(HL.w), true);
							break;
						case 0x8F://adc a,a
							AddA(A, true);
							break;
						#endregion
						#region Ops 0x90-0x9F (checked 2011/11/10)
						case 0x90://sub a,b
							SubA(BC.hi, false);
							break;
						case 0x91://sub a,c
							SubA(BC.lo, false);
							break;
						case 0x92://sub a,d
							SubA(DE.hi, false);
							break;
						case 0x93://sub a,e
							SubA(DE.lo, false);
							break;
						case 0x94://sub a,h
							SubA(HL.hi, false);
							break;
						case 0x95://sub a,l
							SubA(HL.lo, false);
							break;
						case 0x96://sub a,[hl]
							SubA(Read(HL.w), false);
							break;
						case 0x97://sub a,a
							SubA(A, false);
							break;
						case 0x98://sbc a,b
							SubA(BC.hi, true);
							break;
						case 0x99://sbc a,c
							SubA(BC.lo, true);
							break;
						case 0x9A://sbc a,d
							SubA(DE.hi, true);
							break;
						case 0x9B://sbc a,e
							SubA(DE.lo, true);
							break;
						case 0x9C://sbc a,h
							SubA(HL.hi, true);
							break;
						case 0x9D://sbc a,l
							SubA(HL.lo, true);
							break;
						case 0x9E://sbc a,[hl]
							SubA(Read(HL.w), true);
							break;
						case 0x9F://sbc a,a
							SubA(A, true);
							break;
						#endregion
						#region Ops 0xA0-0xAF (checked 2011/11/10)
						case 0xA0://and a,b
							AndA(BC.hi);
							break;
						case 0xA1://and a,c
							AndA(BC.lo);
							break;
						case 0xA2://and a,d
							AndA(DE.hi);
							break;
						case 0xA3://and a,e
							AndA(DE.lo);
							break;
						case 0xA4://and a,h
							AndA(HL.hi);
							break;
						case 0xA5://and a,l
							AndA(HL.lo);
							break;
						case 0xA6://and a,[hl]
							AndA(Read(HL.w));
							break;
						case 0xA7://and a,a
							AndA(A);
							break;
						case 0xA8://xor a,b
							XorA(BC.hi);
							break;
						case 0xA9://xor a,c
							XorA(BC.lo);
							break;
						case 0xAA://xor a,d
							XorA(DE.hi);
							break;
						case 0xAB://xor a,e
							XorA(DE.lo);
							break;
						case 0xAC://xor a,h
							XorA(HL.hi);
							break;
						case 0xAD://xor a,l
							XorA(HL.lo);
							break;
						case 0xAE://xor a,[hl]
							XorA(Read(HL.w));
							break;
						case 0xAF://xor a,a
							XorA(A);
							break;
						#endregion
						#region Ops 0xB0-0xBF (checked 2011/11/10)
						case 0xB0://or a,b
							OrA(BC.hi);
							break;
						case 0xB1://or a,c
							OrA(BC.lo);
							break;
						case 0xB2://or a,d
							OrA(DE.hi);
							break;
						case 0xB3://or a,e
							OrA(DE.lo);
							break;
						case 0xB4://or a,h
							OrA(HL.hi);
							break;
						case 0xB5://or a,l
							OrA(HL.lo);
							break;
						case 0xB6://or a,[hl]
							OrA(Read(HL.w));
							break;
						case 0xB7://or a,a
							OrA(A);
							break;
						case 0xB8://cp a,b
							CpA(BC.hi);
							break;
						case 0xB9://cp a,c
							CpA(BC.lo);
							break;
						case 0xBA://cp a,d
							CpA(DE.hi);
							break;
						case 0xBB://cp a,e
							CpA(DE.lo);
							break;
						case 0xBC://cp a,h
							CpA(HL.hi);
							break;
						case 0xBD://cp a,l
							CpA(HL.lo);
							break;
						case 0xBE://cp a,[hl]
							CpA(Read(HL.w));
							break;
						case 0xBF://cp a,a
							CpA(A);
							break;
						#endregion
						#region Ops 0xC0-0xCF (checked 2011/11/10)
						case 0xC0://ret nz
							CheckedReturn(!IsZero);
							break;
						case 0xC1://pop bc
							Pop(ref BC.w);
							break;
						case 0xC2://jp nz,nnnn
							Jump(!IsZero);
							break;
						case 0xC3://jp nnnn
							Jump(true);
							break;
						case 0xC4://call nz,nnnn
							Call(!IsZero);
							break;
						case 0xC5://push bc
							Push(BC.w);
							break;
						case 0xC6://add a,nn
							AddA(ReadPC(), false);
							break;
						case 0xC7://rst $00
							RST(0x00);
							break;
						case 0xC8://ret z
							CheckedReturn(IsZero);
							break;
						case 0xC9://ret
							Return(false);
							break;
						case 0xCA://jp z,nnnn
							Jump(IsZero);
							break;
						case 0xCB://CB Instruction
							CBstep();
							break;
						case 0xCC://call z,nnnn
							Call(IsZero);
							break;
						case 0xCD://call nnnn
							Call(true);
							break;
						case 0xCE://adc a, nn
							AddA(ReadPC(), true);
							break;
						case 0xCF://rst $08
							RST(0x08);
							break;
						#endregion
						#region Ops 0xD0-0xDF (checked 2011/11/10)
						case 0xD0://ret nc
							CheckedReturn(!IsCarry);
							break;
						case 0xD1://pop de
							Pop(ref DE.w);
							break;
						case 0xD2://jp nc, nnnn
							Jump(!IsCarry);
							break;
						case 0xD3://----
							break;
						case 0xD4://call nc, nnnn
							Call(!IsCarry);
							break;
						case 0xD5://push de
							Push(DE.w);
							break;
						case 0xD6://sub nn
							SubA(ReadPC(), false);
							break;
						case 0xD7://rst $10
							RST(0x10);
							break;
						case 0xD8://ret c
							CheckedReturn(IsCarry);
							break;
						case 0xD9://reti
							Return(true);
							break;
						case 0xDA://jp c,nnnn
							Jump(IsCarry);
							break;
						case 0xDB://---
							break;
						case 0xDC://call c,nnnn
							Call(IsCarry);
							break;
						case 0xDD://---
							break;
						case 0xDE://sbc a,nn
							SubA(ReadPC(), true);
							break;
						case 0xDF://rst $18
							RST(0x18);
							break;
						#endregion
						#region Ops 0xE0-0xEF (checked 2011/11/10)
						case 0xE0://ld [$ffnn],a
							ushort ldaddress = 0xFF00;
							ldaddress |= ReadPC();
							Write(ldaddress, A);
							break;
						case 0xE1://pop hl
							Pop(ref HL.w);
							break;
						case 0xE2://ld [c],a
							ushort ldcaddress = 0xFF00;
							ldcaddress |= BC.lo;
							Write(ldcaddress, A);
							break;
						case 0xE3://---
							break;
						case 0xE4://---
							break;
						case 0xE5://push hl
							Push(HL.w);
							break;
						case 0xE6://and a,nn
							AndA(ReadPC());
							break;
						case 0xE7://rst $20
							RST(0x20);
							break;
						case 0xE8://add sp,nn
							AddSP(ReadPC());
							break;
						case 0xE9://jp hl
							PC.w = HL.w;
							break;
						case 0xEA://ld [$nnnn],a
							Register temp = new Register();
							temp.lo = ReadPC();
							temp.hi = ReadPC();
							Write(temp.w, A);
							break;
						case 0xEB://---
							break;
						case 0xEC://---
							break;
						case 0xED://---
							break;
						case 0xEE://xor nn
							XorA(ReadPC());
							break;
						case 0xEF://rst $28
							RST(0x28);
							break;
						#endregion
						#region Ops 0xF0-0xFF (checked 2011/11/10)
						case 0xF0://ld a,[$ffnn]
							ushort nnAddress = 0xFF00;
							nnAddress |= ReadPC();
							A = Read(nnAddress);
							break;
						case 0xF1://pop af
							Register tempPop = new Register();
							Pop(ref tempPop.w);
							AF = tempPop;
							break;
						case 0xF2://ld a,[c]
							ushort ldrcaddress = 0xFF00;
							ldrcaddress |= BC.lo;
							A = Read(ldrcaddress);
							break;
						case 0xF3://di
							InterruptMasterEnable = false;
							break;
						case 0xF4://--
							break;
						case 0xF5://push af
							Push(AF.w);
							break;
						case 0xF6://or nn
							OrA(ReadPC());
							break;
						case 0xF7://rst $30
							RST(0x30);
							break;
						case 0xF8://ldhl sp,nn
							byte offHL = ReadPC();
							int tempAHL = SP.w + twosTable[offHL];
							int tempAHX = SP.w ^ twosTable[offHL] ^ tempAHL;
							IsHalfCarry = (tempAHX & 0x1000) != 0;
							IsCarry = (tempAHX & 0x10000) != 0;
							IsNegativeOp = false;
							IsZero = false;
							HL.w = (ushort)tempAHL;
							CycleCounter += 4;
							break;
						case 0xF9://ld sp,hl
							SP.w = HL.w;
							CycleCounter += 4;
							break;
						case 0xFA://ld a,[nnnn]
							Register tempLoc = new Register();
							tempLoc.lo = ReadPC();
							tempLoc.hi = ReadPC();
							A = Read(tempLoc.w);
							break;
						case 0xFB://ei
							InterruptMasterEnable = true;
							break;
						case 0xFC://--
							break;
						case 0xFD://--
							break;
						case 0xFE://cp a,nn
							CpA(ReadPC());
							break;
						case 0xFF://rst $38
							RST(0x38);
							break;
						#endregion
					}
				}
				cycles -= CycleCounter;
				mmu.UpdateCounter(CycleCounter);
			}
			FrameDone = true;
		}

		public void CBstep()
		{
			switch (ReadPC())
			{
				#region RLC (checked 2011/11/10)
				case 0x00://rlc b
					RLC(ref BC.hi);
					break;
				case 0x01://rlc c
					RLC(ref BC.lo);
					break;
				case 0x02://rlc d
					RLC(ref DE.hi);
					break;
				case 0x03://rlc e
					RLC(ref DE.lo);
					break;
				case 0x04://rlc h
					RLC(ref HL.hi);
					break;
				case 0x05://rlc l
					RLC(ref HL.lo);
					break;
				case 0x06://rlc [hl]
					byte rlchl = Read(HL.w);
					RLC(ref rlchl);
					Write(HL.w, rlchl);
					break;
				case 0x07://rlc a
					RLC(ref A);
					break;
				#endregion
				#region RRC (checked 2011/11/10)
				case 0x08://rrc b
					RRC(ref BC.hi);
					break;
				case 0x09://rrc c
					RRC(ref BC.lo);
					break;
				case 0x0A://rrc d
					RRC(ref DE.hi);
					break;
				case 0x0B://rrc e
					RRC(ref DE.lo);
					break;
				case 0x0C://rrc h
					RRC(ref HL.hi);
					break;
				case 0x0D://rrc l
					RRC(ref HL.lo);
					break;
				case 0x0E://rrc [hl]
					byte rrchl = Read(HL.w);
					RRC(ref rrchl);
					Write(HL.w, rrchl);
					break;
				case 0x0F://rrc a
					RRC(ref A);
					break;
				#endregion
				#region RL (checked 2011/11/10)
				case 0x10://rl b
					RL(ref BC.hi);
					break;
				case 0x11://rl c
					RL(ref BC.lo);
					break;
				case 0x12://rl d
					RL(ref DE.hi);
					break;
				case 0x13://rl e
					RL(ref DE.lo);
					break;
				case 0x14://rl h
					RL(ref HL.hi);
					break;
				case 0x15://rl l
					RL(ref HL.lo);
					break;
				case 0x16://rl [hl]
					byte rlhl = Read(HL.w);
					RL(ref rlhl);
					Write(HL.w, rlhl);
					break;
				case 0x17://rl a
					RL(ref A);
					break;
				#endregion
				#region RR (checked 2011/11/10)
				case 0x18://rr b
					RR(ref BC.hi);
					break;
				case 0x19://rr c
					RR(ref BC.lo);
					break;
				case 0x1A://rr d
					RR(ref DE.hi);
					break;
				case 0x1B://rr e
					RR(ref DE.lo);
					break;
				case 0x1C://rr h
					RR(ref HL.hi);
					break;
				case 0x1D://rr l
					RR(ref HL.lo);
					break;
				case 0x1E://rr [hl]
					byte rrhl = Read(HL.w);
					RR(ref rrhl);
					Write(HL.w, rrhl);
					break;
				case 0x1F://rr a
					RR(ref A);
					break;
				#endregion
				#region SLA (checked 2011/11/10)
				case 0x20://sla b
					SLA(ref BC.hi);
					break;
				case 0x21://sla c
					SLA(ref BC.lo);
					break;
				case 0x22://sla d
					SLA(ref DE.hi);
					break;
				case 0x23://sla e
					SLA(ref DE.lo);
					break;
				case 0x24://sla h
					SLA(ref HL.hi);
					break;
				case 0x25://sla l
					SLA(ref HL.lo);
					break;
				case 0x26://sla [hl]
					byte slahl = Read(HL.w);
					SLA(ref slahl);
					Write(HL.w, slahl);
					break;
				case 0x27://sla a
					SLA(ref A);
					break;
				#endregion
				#region SRA (checked 2011/11/10)
				case 0x28://sra b
					SRA(ref BC.hi);
					break;
				case 0x29://sra c
					SRA(ref BC.lo);
					break;
				case 0x2A://sra d
					SRA(ref DE.hi);
					break;
				case 0x2B://sra e
					SRA(ref DE.lo);
					break;
				case 0x2C://sra h
					SRA(ref HL.hi);
					break;
				case 0x2D://sra l
					SRA(ref HL.lo);
					break;
				case 0x2E://sra [hl]
					byte srahl = Read(HL.w);
					SRA(ref srahl);
					Write(HL.w, srahl);
					break;
				case 0x2F://sra a
					SRA(ref A);
					break;
				#endregion
				#region Swap (checked 2011/11/10)
				case 0x30://swap b
					Swap(ref BC.hi);
					break;
				case 0x31://swap c
					Swap(ref BC.lo);
					break;
				case 0x32://swap d
					Swap(ref DE.hi);
					break;
				case 0x33://swap e
					Swap(ref DE.lo);
					break;
				case 0x34://swap h
					Swap(ref HL.hi);
					break;
				case 0x35://swap l
					Swap(ref HL.lo);
					break;
				case 0x36://swap [hl]
					byte swaphl = Read(HL.w);
					Swap(ref swaphl);
					Write(HL.w, swaphl);
					break;
				case 0x37://swap a
					Swap(ref A);
					break;
				#endregion
				#region SRL (checked 2011/11/10)
				case 0x38://srl b
					SRL(ref BC.hi);
					break;
				case 0x39://srl c
					SRL(ref BC.lo);
					break;
				case 0x3A://srl d
					SRL(ref DE.hi);
					break;
				case 0x3B://srl e
					SRL(ref DE.lo);
					break;
				case 0x3C://srl h
					SRL(ref HL.hi);
					break;
				case 0x3D://srl l
					SRL(ref HL.lo);
					break;
				case 0x3E://srl [hl]
					byte srlHL = Read(HL.w);
					SRL(ref srlHL);
					Write(HL.w, srlHL);
					break;
				case 0x3F://srl a
					SRL(ref A);
					break;
				#endregion
				#region Bit (checked 2011/11/10)
				case 0x40://bit 0, b
					Bit(BC.hi, 0);
					break;
				case 0x41://bit 0, c
					Bit(BC.lo, 0);
					break;
				case 0x42://bit 0, d
					Bit(DE.hi, 0);
					break;
				case 0x43://bit 0, e
					Bit(DE.lo, 0);
					break;
				case 0x44://bit 0, h
					Bit(HL.hi, 0);
					break;
				case 0x45://bit 0, l
					Bit(HL.lo, 0);
					break;
				case 0x46://bit 0, [hl]
					Bit(Read(HL.w), 0);
					break;
				case 0x47://bit 0, a
					Bit(A, 0);
					break;
				case 0x48://bit 1, b
					Bit(BC.hi, 1);
					break;
				case 0x49://bit 1, c
					Bit(BC.lo, 1);
					break;
				case 0x4A://bit 1, d
					Bit(DE.hi, 1);
					break;
				case 0x4B://bit 1, e
					Bit(DE.lo, 1);
					break;
				case 0x4C://bit 1, h
					Bit(HL.hi, 1);
					break;
				case 0x4D://bit 1, l
					Bit(HL.lo, 1);
					break;
				case 0x4E://bit 1, hl
					Bit(Read(HL.w), 1);
					break;
				case 0x4F://bit 1, a
					Bit(A, 1);
					break;
				case 0x50://bit 2, b
					Bit(BC.hi, 2);
					break;
				case 0x51://bit 2, c
					Bit(BC.lo, 2);
					break;
				case 0x52://bit 2, d
					Bit(DE.hi, 2);
					break;
				case 0x53://bit 2, e
					Bit(DE.lo, 2);
					break;
				case 0x54://bit 2, h
					Bit(HL.hi, 2);
					break;
				case 0x55://bit 2, l
					Bit(HL.lo, 2);
					break;
				case 0x56://bit 2, hl
					Bit(Read(HL.w), 2);
					break;
				case 0x57://bit 2, a
					Bit(A, 2);
					break;
				case 0x58://bit 3, b
					Bit(BC.hi, 3);
					break;
				case 0x59://bit 3, c
					Bit(BC.lo, 3);
					break;
				case 0x5A://bit 3, d
					Bit(DE.hi, 3);
					break;
				case 0x5B://bit 3, e
					Bit(DE.lo, 3);
					break;
				case 0x5C://bit 3, h
					Bit(HL.hi, 3);
					break;
				case 0x5D://bit 3, l
					Bit(HL.lo, 3);
					break;
				case 0x5E://bit 3, hl
					Bit(Read(HL.w), 3);
					break;
				case 0x5F://bit 3, a
					Bit(A, 3);
					break;
				case 0x60://bit 4, b
					Bit(BC.hi, 4);
					break;
				case 0x61://bit 4, c
					Bit(BC.lo, 4);
					break;
				case 0x62://bit 4, d
					Bit(DE.hi, 4);
					break;
				case 0x63://bit 4, e
					Bit(DE.lo, 4);
					break;
				case 0x64://bit 4, h
					Bit(HL.hi, 4);
					break;
				case 0x65://bit 4, l
					Bit(HL.lo, 4);
					break;
				case 0x66://bit 4, hl
					Bit(Read(HL.w), 4);
					break;
				case 0x67://bit 4, a
					Bit(A, 4);
					break;
				case 0x68://bit 5, b
					Bit(BC.hi, 5);
					break;
				case 0x69://bit 5, c
					Bit(BC.lo, 5);
					break;
				case 0x6A://bit 5, d
					Bit(DE.hi, 5);
					break;
				case 0x6B://bit 5, e
					Bit(DE.lo, 5);
					break;
				case 0x6C://bit 5, h
					Bit(HL.hi, 5);
					break;
				case 0x6D://bit 5, l
					Bit(HL.lo, 5);
					break;
				case 0x6E://bit 5, hl
					Bit(Read(HL.w), 5);
					break;
				case 0x6F://bit 5, a
					Bit(A, 5);
					break;
				case 0x70://bit 6, b
					Bit(BC.hi, 6);
					break;
				case 0x71://bit 6, c
					Bit(BC.lo, 6);
					break;
				case 0x72://bit 6, d
					Bit(DE.hi, 6);
					break;
				case 0x73://bit 6, e
					Bit(DE.lo, 6);
					break;
				case 0x74://bit 6, h
					Bit(HL.hi, 6);
					break;
				case 0x75://bit 6, l
					Bit(HL.lo, 6);
					break;
				case 0x76://bit 6, hl
					Bit(Read(HL.w), 6);
					break;
				case 0x77://bit 6, a
					Bit(A, 6);
					break;
				case 0x78://bit 7, b
					Bit(BC.hi, 7);
					break;
				case 0x79://bit 7, c
					Bit(BC.lo, 7);
					break;
				case 0x7A://bit 7, d
					Bit(DE.hi, 7);
					break;
				case 0x7B://bit 7, e
					Bit(DE.lo, 7);
					break;
				case 0x7C://bit 7, h
					Bit(HL.hi, 7);
					break;
				case 0x7D://bit 7, l
					Bit(HL.lo, 7);
					break;
				case 0x7E://bit 7, [hl]
					Bit(Read(HL.w), 7);
					break;
				case 0x7F://bit 7, a
					Bit(A, 7);
					break;
				#endregion
				#region Reset (checked 2011/11/10)
				case 0x80://res 0, b
					Reset(ref BC.hi, 0);
					break;
				case 0x81://res 0, c
					Reset(ref BC.lo, 0);
					break;
				case 0x82://res 0, d
					Reset(ref DE.hi, 0);
					break;
				case 0x83://res 0, e
					Reset(ref DE.lo, 0);
					break;
				case 0x84://res 0, h
					Reset(ref HL.hi, 0);
					break;
				case 0x85://res 0, l
					Reset(ref HL.lo, 0);
					break;
				case 0x86://res 0, [hl]
					Write(HL.w, (byte)(Read(HL.w) & RESET_0));
					break;
				case 0x87://res 0, a
					Reset(ref A, 0);
					break;
				case 0x88://res 1, b
					Reset(ref BC.hi, 1);
					break;
				case 0x89://res 1, c
					Reset(ref BC.lo, 1);
					break;
				case 0x8A://res 1, d
					Reset(ref DE.hi, 1);
					break;
				case 0x8B://res 1, e
					Reset(ref DE.lo, 1);
					break;
				case 0x8C://res 1, h
					Reset(ref HL.hi, 1);
					break;
				case 0x8D://res 1, l
					Reset(ref HL.lo, 1);
					break;
				case 0x8E://res 1, [hl]
					Write(HL.w, (byte)(Read(HL.w) & RESET_1));
					break;
				case 0x8F://res 1, a
					Reset(ref A, 1);
					break;
				case 0x90://res 2, b
					Reset(ref BC.hi, 2);
					break;
				case 0x91://res 2, c
					Reset(ref BC.lo, 2);
					break;
				case 0x92://res 2, d
					Reset(ref DE.hi, 2);
					break;
				case 0x93://res 2, e
					Reset(ref DE.lo, 2);
					break;
				case 0x94://res 2, h
					Reset(ref HL.hi, 2);
					break;
				case 0x95://res 2, l
					Reset(ref HL.lo, 2);
					break;
				case 0x96://res 2, [hl]
					Write(HL.w, (byte)(Read(HL.w) & RESET_2));
					break;
				case 0x97://res 2, a
					Reset(ref A, 2);
					break;
				case 0x98://res 3, b
					Reset(ref BC.hi, 3);
					break;
				case 0x99://res 3, c
					Reset(ref BC.lo, 3);
					break;
				case 0x9A://res 3, d
					Reset(ref DE.hi, 3);
					break;
				case 0x9B://res 3, e
					Reset(ref DE.lo, 3);
					break;
				case 0x9C://res 3, h
					Reset(ref HL.hi, 3);
					break;
				case 0x9D://res 3, l
					Reset(ref HL.lo, 3);
					break;
				case 0x9E://res 3, [hl]
					Write(HL.w, (byte)(Read(HL.w) & RESET_3));
					break;
				case 0x9F://res 3, a
					Reset(ref A, 3);
					break;
				case 0xA0://res 4, b
					Reset(ref BC.hi, 4);
					break;
				case 0xA1://res 4, c
					Reset(ref BC.lo, 4);
					break;
				case 0xA2://res 4, d
					Reset(ref DE.hi, 4);
					break;
				case 0xA3://res 4, e
					Reset(ref DE.lo, 4);
					break;
				case 0xA4://res 4, h
					Reset(ref HL.hi, 4);
					break;
				case 0xA5://res 4, l
					Reset(ref HL.lo, 4);
					break;
				case 0xA6://res 4, [hl]
					Write(HL.w, (byte)(Read(HL.w) & RESET_4));
					break;
				case 0xA7://res 4, a
					Reset(ref A, 4);
					break;
				case 0xA8://res 5, b
					Reset(ref BC.hi, 5);
					break;
				case 0xA9://res 5, c
					Reset(ref BC.lo, 5);
					break;
				case 0xAA://res 5, d
					Reset(ref DE.hi, 5);
					break;
				case 0xAB://res 5, e
					Reset(ref DE.lo, 5);
					break;
				case 0xAC://res 5, h
					Reset(ref HL.hi, 5);
					break;
				case 0xAD://res 5, l
					Reset(ref HL.lo, 5);
					break;
				case 0xAE://res 5, [hl]
					Write(HL.w, (byte)(Read(HL.w) & RESET_5));
					break;
				case 0xAF://res 5, a
					Reset(ref A, 5);
					break;
				case 0xB0://res 6, b
					Reset(ref BC.hi, 6);
					break;
				case 0xB1://res 6, c
					Reset(ref BC.lo, 6);
					break;
				case 0xB2://res 6, d
					Reset(ref DE.hi, 6);
					break;
				case 0xB3://res 6, e
					Reset(ref DE.lo, 6);
					break;
				case 0xB4://res 6, h
					Reset(ref HL.hi, 6);
					break;
				case 0xB5://res 6, l
					Reset(ref HL.lo, 6);
					break;
				case 0xB6://res 6, [hl]
					Write(HL.w, (byte)(Read(HL.w) & RESET_6));
					break;
				case 0xB7://res 6, a
					Reset(ref A, 6);
					break;
				case 0xB8://res 7, b
					Reset(ref BC.hi, 7);
					break;
				case 0xB9://res 7, c
					Reset(ref BC.lo, 7);
					break;
				case 0xBA://res 7, d
					Reset(ref DE.hi, 7);
					break;
				case 0xBB://res 7, e
					Reset(ref DE.lo, 7);
					break;
				case 0xBC://res 7, h
					Reset(ref HL.hi, 7);
					break;
				case 0xBD://res 7, l
					Reset(ref HL.lo, 7);
					break;
				case 0xBE://res 7, [hl]
					Write(HL.w, (byte)(Read(HL.w) & RESET_7));
					break;
				case 0xBF://res 7, a
					Reset(ref A, 7);
					break;
				#endregion
				#region Set (checked 2011/11/10)
				case 0xC0://set 0, b
					Set(ref BC.hi, 0);
					break;
				case 0xC1://set 0, c
					Set(ref BC.lo, 0);
					break;
				case 0xC2://set 0, d
					Set(ref DE.hi, 0);
					break;
				case 0xC3://set 0, e
					Set(ref DE.lo, 0);
					break;
				case 0xC4://set 0, h
					Set(ref HL.hi, 0);
					break;
				case 0xC5://set 0, l
					Set(ref HL.lo, 0);
					break;
				case 0xC6://set 0, [hl]
					Write(HL.w, (byte)(Read(HL.w) | SET_0));
					break;
				case 0xC7://set 0, a
					Set(ref A, 0);
					break;
				case 0xC8://set 1, b
					Set(ref BC.hi, 1);
					break;
				case 0xC9://set 1, c
					Set(ref BC.lo, 1);
					break;
				case 0xCA://set 1, d
					Set(ref DE.hi, 1);
					break;
				case 0xCB://set 1, e
					Set(ref DE.lo, 1);
					break;
				case 0xCC://set 1, h
					Set(ref HL.hi, 1);
					break;
				case 0xCD://set 1, l
					Set(ref HL.lo, 1);
					break;
				case 0xCE://set 1, [hl]
					Write(HL.w, (byte)(Read(HL.w) | SET_1));
					break;
				case 0xCF://set 1, a
					Set(ref A, 1);
					break;
				case 0xD0://set 2, b
					Set(ref BC.hi, 2);
					break;
				case 0xD1://set 2, c
					Set(ref BC.lo, 2);
					break;
				case 0xD2://set 2, d
					Set(ref DE.hi, 2);
					break;
				case 0xD3://set 2, e
					Set(ref DE.lo, 2);
					break;
				case 0xD4://set 2, h
					Set(ref HL.hi, 2);
					break;
				case 0xD5://set 2, l
					Set(ref HL.lo, 2);
					break;
				case 0xD6://set 2, [hl]
					Write(HL.w, (byte)(Read(HL.w) | SET_2));
					break;
				case 0xD7://set 2, a
					Set(ref A, 2);
					break;
				case 0xD8://set 3, b
					Set(ref BC.hi, 3);
					break;
				case 0xD9://set 3, c
					Set(ref BC.lo, 3);
					break;
				case 0xDA://set 3, d
					Set(ref DE.hi, 3);
					break;
				case 0xDB://set 3, e
					Set(ref DE.lo, 3);
					break;
				case 0xDC://set 3, h
					Set(ref HL.hi, 3);
					break;
				case 0xDD://set 3, l
					Set(ref HL.lo, 3);
					break;
				case 0xDE://set 3, [hl]
					Write(HL.w, (byte)(Read(HL.w) | SET_3));
					break;
				case 0xDF://set 3, a
					Set(ref A, 3);
					break;
				case 0xE0://set 4, b
					Set(ref BC.hi, 4);
					break;
				case 0xE1://set 4, c
					Set(ref BC.lo, 4);
					break;
				case 0xE2://set 4, d
					Set(ref DE.hi, 4);
					break;
				case 0xE3://set 4, e
					Set(ref DE.lo, 4);
					break;
				case 0xE4://set 4, h
					Set(ref HL.hi, 4);
					break;
				case 0xE5://set 4, l
					Set(ref HL.lo, 4);
					break;
				case 0xE6://set 4, [hl]
					Write(HL.w, (byte)(Read(HL.w) | SET_4));
					break;
				case 0xE7://set 4, a
					Set(ref A, 4);
					break;
				case 0xE8://set 5, b
					Set(ref BC.hi, 5);
					break;
				case 0xE9://set 5, c
					Set(ref BC.lo, 5);
					break;
				case 0xEA://set 5, d
					Set(ref DE.hi, 5);
					break;
				case 0xEB://set 5, e
					Set(ref DE.lo, 5);
					break;
				case 0xEC://set 5, h
					Set(ref HL.hi, 5);
					break;
				case 0xED://set 5, l
					Set(ref HL.lo, 5);
					break;
				case 0xEE://set 5, [hl]
					Write(HL.w, (byte)(Read(HL.w) | SET_5));
					break;
				case 0xEF://set 5, a
					Set(ref A, 5);
					break;
				case 0xF0://set 6, b
					Set(ref BC.hi, 6);
					break;
				case 0xF1://set 6, c
					Set(ref BC.lo, 6);
					break;
				case 0xF2://set 6, d
					Set(ref DE.hi, 6);
					break;
				case 0xF3://set 6, e
					Set(ref DE.lo, 6);
					break;
				case 0xF4://set 6, h
					Set(ref HL.hi, 6);
					break;
				case 0xF5://set 6, l
					Set(ref HL.lo, 6);
					break;
				case 0xF6://set 6, [hl]
					Write(HL.w, (byte)(Read(HL.w) | SET_6));
					break;
				case 0xF7://set 6, a
					Set(ref A, 6);
					break;
				case 0xF8://set 7, b
					Set(ref BC.hi, 7);
					break;
				case 0xF9://set 7, c
					Set(ref BC.lo, 7);
					break;
				case 0xFA://set 7, d
					Set(ref DE.hi, 7);
					break;
				case 0xFB://set 7, e
					Set(ref DE.lo, 7);
					break;
				case 0xFC://set 7, h
					Set(ref HL.hi, 7);
					break;
				case 0xFD://set 7, l
					Set(ref HL.lo, 7);
					break;
				case 0xFE://set 7, [hl]
					Write(HL.w, (byte)(Read(HL.w) | SET_7));
					break;
				case 0xFF://set 7, a
					Set(ref A, 7);
					break;
				#endregion
			}
		}//Checked 2011/11/10

		private bool CheckInterrupts(byte flagStatus)
		{
			if (flagStatus != 0)
			{
				int sPoint = 0;
				while (((1 << sPoint) & flagStatus) == 0 && sPoint < 5)
				{
					sPoint++;
				}
				Push(PC.w);
				PCChange(interruptVectors[sPoint]);
				mmu.ResetInterruptFlag(sPoint);
				InterruptMasterEnable = false;
				return true;
			}
			else return false;
		}

		#region Instruction Implememtation
		private byte Read(ushort src)
		{
			//Reads take 4 cycles
			CycleCounter += 4;
			return mmu.Read(src);
		}//Done, cycle good 11/1/11
		private byte ReadPC()
		{
			return Read(PC.w++);
		}
		private void Write(ushort dest, byte data)
		{
			//Writes take 4 cycles.
			CycleCounter += 4;
			mmu.Write(dest, data);
		}//Done, cycle good 11/1/11
		private void Load16Immediate(ref Register reg)
		{
			reg.lo = ReadPC();
			reg.hi = ReadPC();
		}
		private void Inc(ref byte refreg)
		{
			int temp = refreg + 1;
			int tempover = refreg ^ 1 ^ temp;
			IsHalfCarry = ((tempover & 0x10) != 0);
			IsNegativeOp = false;
			refreg++;
			IsZero = (refreg == 0);
		}//Done, pending checks 11/1/11
		private void Inc16(ref ushort reg)
		{
			reg++;
			CycleCounter += 4;
		}//Done, cycle good, pending checks 11/2/11
		private void Dec(ref byte refreg)
		{
			int temp = refreg - 1;
			int tempover = refreg ^ ~(1) ^ temp;
			IsHalfCarry = ((tempover & 0x10) != 0);
			IsNegativeOp = true;
			refreg--;
			IsZero = (refreg == 0);
		}//Done, pending checks 11/1/11
		private void Dec16(ref ushort reg)
		{
			reg--;
			CycleCounter += 4;
		}//Done, cycle good, pending checks 11/2/11
		private void AddA(byte add, bool addCarry)
		{
			byte tempAdd = (byte)(add + (addCarry ? IsCarry ? 1 : 0 : 0));
			int temp = A + tempAdd;
			int tempover = A ^ tempAdd ^ temp;
			IsHalfCarry = ((tempover & 0x10) != 0);
			IsCarry = ((tempover & 0x100) != 0);
			IsNegativeOp = false;
			A += tempAdd;
			IsZero = ( A == 0);
		}//Done, pending checks 11/10/11
		private void SubA(byte sub, bool subCarry)
		{
			byte tempsub = (byte)(sub + (subCarry ? IsCarry ? 1 : 0 : 0));
			int temp = A - tempsub;
			int tempover = A ^ tempsub ^ temp;
			IsHalfCarry = ((tempover & 0x10) != 0);
			IsCarry = ((tempover & 0x100) != 0);
			IsNegativeOp = true;
			A -= tempsub;
			IsZero = (A == 0);
		}//Done, pending checks 11/10/11
		private void AddHL(ushort refreg)
		{
			int temp = HL.w + refreg;
			int tempover = HL.w ^ refreg ^ temp;
			IsHalfCarry = ((tempover & 0x1000) != 0);
			IsCarry = ((tempover & 0x10000) != 0);
			IsNegativeOp = false;
			HL.w += refreg;
			CycleCounter += 4;
		}//Done, cycle good, pending checks 11/1/11
		private void AddSP(byte refreg)
		{
			int temp = SP.w + twosTable[refreg];
			int tempover = SP.w ^ twosTable[refreg] ^ temp;
			IsHalfCarry = ((tempover & 0x1000) != 0);
			IsCarry = ((tempover & 0x10000) != 0);
			IsNegativeOp = false;
			IsZero = false;
			SP.w += (ushort)twosTable[refreg];
			CycleCounter += 8;
		}//Done, cycle good, pending checks 11/1/11
		private void AndA(byte val)
		{
			A &= val;
			IsZero = A == 0;
			IsNegativeOp = false;
			IsHalfCarry = true;
			IsCarry = false;
		}//Done, 11/2/11
		private void OrA(byte val)
		{
			A |= val;
			IsZero = A == 0;
			IsNegativeOp = false;
			IsHalfCarry = false;
			IsCarry = false;
		}//Done, 11/2/11
		private void XorA(byte val)
		{
			A ^= val;
			IsZero = A == 0;
			IsNegativeOp = false;
			IsHalfCarry = false;
			IsCarry = false;
		}//Done, 11/2/11
		private void CpA(byte val)
		{
			IsNegativeOp = true;
			int temp = A - val;
			int tempX = A ^ val ^ temp;
			IsHalfCarry = (tempX & 0x10) != 0;
			IsCarry = (tempX & 0x100) != 0;
			IsZero = (temp & 0xFF) == 0;
		}//Done, 11/2/11
		private void Swap(ref byte val)
		{
			IsNegativeOp = IsHalfCarry = IsCarry = false;
			IsZero = val == 0;
			val = (byte)((val << 4) | (val >> 4));
		}//Done, 11/1/11
		private void Set(ref byte val, int bit)
		{
			byte[] setBits = new byte[8]
			{
				1, 2, 4, 8, 16, 32, 64, 128
			};
			val |= setBits[bit];
		}//Done, 11/1/11
		private void Reset(ref byte val, int bit)
		{
			byte[] resetBits = new byte[8]
			{
				0xFE, 0xFD, 0xFB, 0xF7, 0xEF, 0xDF, 0xBF, 0x7F
			};
			val &= resetBits[bit];
		}//Done, 11/1/11
		private void Bit(byte val, int bit)
		{
			IsNegativeOp = false;
			IsHalfCarry = true;
			IsZero = (val & (1 << bit)) == 0;
		}//Done, 11/1/11
		private void RL(ref byte val)
		{
			byte carry = (byte)(IsCarry ? 1 : 0);
			IsCarry = (val & 0x80) != 0;
			val = (byte)(val << 1 | carry);
			IsZero = val == 0;
		}//Done, 11/1/11
		private void RR(ref byte val)
		{
			byte carry = (byte)(IsCarry ? 0x80 : 0);
			IsCarry = (val & 0x1) != 0;
			val = (byte)(val >> 1 | carry);
			IsZero = val == 0;
		}//Done, 11/1/11
		private void RLC(ref byte val)
		{
			IsCarry = (val & 0x80) != 0;
			IsNegativeOp = false;
			IsHalfCarry = false;
			val = (byte)(val << 1 | val >> 7);
			IsZero = val == 0;
		}//Done, 11/1/11
		private void RRC(ref byte val)
		{
			IsCarry = (val & 0x1) != 0;
			IsNegativeOp = false;
			IsHalfCarry = false;
			val = (byte)(val >> 1 | val << 7);
			IsZero = (val == 0);
		}//Done, 11/1/11
		private void SLA(ref byte val)//Done, Needs Confirmation 11/1/11
		{
			IsCarry = (val & 0x80) != 0;
			IsNegativeOp = false;
			IsHalfCarry = false;
			val <<= 1;
			IsZero = val == 0;
		}
		private void SRA(ref byte val)
		{
			IsCarry = (val & 0x1) != 0;
			IsNegativeOp = false;
			IsHalfCarry = false;
			val = (byte)((val >> 1) | (val & 0x80));
			IsZero = val == 0;
		}//Done, Needs Confirmation 11/1/11
		private void SRL(ref byte val)
		{
			IsCarry = (val & 0x1) != 0;
			IsNegativeOp = false;
			IsHalfCarry = false;
			val >>= 1;
			IsZero = val == 0;
		}//Done, Needs Confirmation 11/1/11
		private void Jump(bool isCondTrue)
		{
			Register jl = new Register();
			jl.lo = ReadPC();
			jl.hi = ReadPC();
			if (isCondTrue)
			{
				PCChange(jl.w);
			}
		}//Done, cycle good, pending check 11/3/11
		private void JumpRelative(bool isCondTrue)
		{
			byte off = ReadPC();
			if (isCondTrue)
			{
				PCChange((ushort)(PC.w + twosTable[off]));
			}
		}//Done, cycle good, pending check 11/3/11
		private void CheckedReturn(bool isCondTrue)
		{
			CycleCounter += 4;
			if (isCondTrue)
			{
				ushort retAddr = 0;
				Pop(ref retAddr);
				PCChange(retAddr);
			}
		}//Done, cycle good, pending check 11/3/11
		private void Return(bool enableInterrupts)
		{
			ushort retAddr = 0;
			Pop(ref retAddr);
			PCChange(retAddr);
			if (enableInterrupts) InterruptMasterEnable = true;
		}//Done, cycle good, pending check 11/3/11
		private void Call(bool isFlag)
		{
			Register rf = new Register();
			rf.lo = ReadPC();
			rf.hi = ReadPC();
			if (isFlag)
			{
				Push(PC.w);
				PCChange(rf.w);
			}
		}//Done, cycle good, pending check 11/3/11
		private void RST(byte jumpVector)
		{
			ushort jumpLoc = (ushort)(jumpVector & 0x38);
			Push(PC.w);
			PCChange(jumpLoc);
		}//Done, cycle good, pending check 11/3/11
		private void Push(ushort pushData)
		{
			Write(SP.w--, (byte)(pushData >> 8));
			Write(SP.w--, (byte)pushData);
		}//Done, cycle good, pending check 11/3/11
		private void Pop(ref ushort popReg)
		{
			ushort x = Read(SP.w++);
			x |= (ushort)(Read(SP.w++) << 8);
			popReg = x;
		}//Done, cycle good, pending check 11/3/11
		private void PCChange(ushort newVal)
		{
			PC.w = newVal;
			CycleCounter += 4;
		}//Done, cycle good, pending check 11/3/11
		#endregion
	}
}