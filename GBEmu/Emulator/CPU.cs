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
	public enum CPUState { Normal, Halt }
	class CPU
	{
		//public const UInt32 DMGCycles = 4194304;
		//public const UInt32 CGB_SingleCycles = 4194300;
		//public const UInt32 SGB_Cycles = 4295454;
		//public const UInt32 CGB_DoubleCycles = 8338000;
		//public const UInt32 DMA_CYCLE = 670;

		public CPUState state;

		private InterruptManager interruptManager;
		
		private bool RepeatLastInstruction;

		#region Interrupt Vectors
		private const ushort IntVector_VBlank = 0x40;
		private const ushort IntVector_LCDC = 0x48;
		private const ushort IntVector_Timer = 0x50;
		private const ushort IntVector_Serial = 0x58;
		private const ushort IntVector_Joypad = 0x60;
		#endregion

		#region Flag Properties
		private bool IsZero
		{
			get
			{
				return (AF.w & 0x80) != 0;
			}
			set
			{
				if (value)
				{
					AF.lo |= 0x80;
				}
				else
				{
					AF.lo &= 0x7F;
				}
			}
		}//Bit 7
		private bool IsNegativeOp
		{
			get
			{
				return (AF.w & 0x40) != 0;
			}
			set
			{
				if (value)
				{
					AF.lo |= 0x40;
				}
				else
				{
					AF.lo &= 0xBF;
				}
			}
		}//Bit 6
		private bool IsHalfCarry
		{
			get
			{
				return (AF.w & 0x20) != 0;
			}
			set
			{
				if (value)
				{
					AF.lo |= 0x20;
				}
				else
				{
					AF.lo &= 0xDF;
				}
			}
		}//Bit 5
		private bool IsCarry
		{
			get
			{
				return (AF.w & 0x10) != 0;
			}
			set
			{
				if (value)
				{
					AF.lo |= 0x10;
				}
				else
				{
					AF.lo &= 0xEF;
				}
			}
		}//Bit 4
		#endregion

		#region Bit Constants
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
		
		#region Registers
		public Register AF;
		public Register BC;
		public Register DE;
		public Register HL;
		public Register PC;
		public Register SP;
		#endregion
		
		public MMU mmu;

		private int CycleCounter;

		public CPU(byte[] inFile, GBEmu.Render.IRenderable screen)
		{
			interruptManager = new InterruptManager();
			mmu = new MMU(inFile, interruptManager, screen);
			InitializeDefaultValues();
		}

		public void InitializeDefaultValues()
		{
			PC.w = 0x0100;
			SP.w = 0xFFFE;
			AF.w = 0x01B0;
			BC.w = 0x0013;
			DE.w = 0x00D8;
			HL.w = 0x014D;
			CycleCounter = 0;
			state = CPUState.Normal;
			RepeatLastInstruction = false;
		}

		public void step(int cycles)
		{
			while (cycles > 0)
			{
				//Stop doesn't increment the PC, and turns off the LCD.
				//Also, speed switch occurs after stop is used.
				CycleCounter = 0;
				CheckInterrupts();
				switch (state)
				{
					case CPUState.Halt:
						CycleCounter += 4;
						break;
					case CPUState.Normal:
						byte inst = ReadPC();
						if (RepeatLastInstruction)//Halt bug
						{
							PC.w--;
							RepeatLastInstruction = false;
						}
						switch (inst)
						{
							#region Ops 0x00-0x0F
							case 0x00://nop
								break;
							case 0x01://ld bc,nnnn
								Load16Immediate(ref BC);
								break;
							case 0x02://ld [bc],a
								Write(BC.w, AF.hi);
								break;
							case 0x03://inc bc
								Inc16(ref BC.w);
								break;
							case 0x04://inc b
								Inc8(ref BC.hi);
								break;
							case 0x05://dec b
								Dec8(ref BC.hi);
								break;
							case 0x06://ld b,nn
								LoadImmediate(ref BC.hi);
								break;
							case 0x07://rlca
								RLC(ref AF.hi);
								break;
							case 0x08://ld [nnnn],sp
								WriteWord(ReadPCWord(), SP.w);
								break;
							case 0x09://add hl,bc
								AddHL(BC.w);
								break;
							case 0x0A://ld a,[bc]
								LoadFromMemory(ref AF.hi, BC.w);
								break;
							case 0x0B://dec bc
								Dec16(ref BC.w);
								break;
							case 0x0C://inc c
								Inc8(ref BC.lo);
								break;
							case 0x0D://dec c
								Dec8(ref BC.lo);
								break;
							case 0x0E://ld c,nn
								LoadImmediate(ref BC.lo);
								break;
							case 0x0F://rrca
								RRC(ref AF.hi);
								break;
							#endregion
							#region Ops 0x10-0x1F
							case 0x10://stop
								Stop();
								break;
							case 0x11://ld de,nnnn
								Load16Immediate(ref DE);
								break;
							case 0x12://ld [de],a
								Write(DE.w, AF.hi);
								break;
							case 0x13://inc de
								Inc16(ref DE.w);
								break;
							case 0x14://inc d
								Inc8(ref DE.hi);
								break;
							case 0x15://dec d
								Dec8(ref DE.hi);
								break;
							case 0x16://ld d,nn
								LoadImmediate(ref DE.hi);
								break;
							case 0x17://rla
								RL(ref AF.hi);
								break;
							case 0x18://jr nn
								JumpRelative(true);
								break;
							case 0x19://add hl,de
								AddHL(DE.w);
								break;
							case 0x1A://ld a,[de]
								LoadFromMemory(ref AF.hi, DE.w);
								break;
							case 0x1B://dec de
								Dec16(ref DE.w);
								break;
							case 0x1C://inc e
								Inc8(ref DE.lo);
								break;
							case 0x1D://dec e
								Dec8(ref DE.lo);
								break;
							case 0x1E://ld e,nn
								LoadImmediate(ref DE.lo);
								break;
							case 0x1F://rra
								RR(ref AF.hi);
								break;
							#endregion
							#region Ops 0x20-0x2F
							case 0x20://jr nz,nn
								JumpRelative(!IsZero);
								break;
							case 0x21://ld hl,nnnn
								Load16Immediate(ref HL);
								break;
							case 0x22://ldi [hl],a
								LoadHL(AF.hi, LoadHLType.Inc);
								break;
							case 0x23://inc hl
								Inc16(ref HL.w);
								break;
							case 0x24://inc h
								Inc8(ref HL.hi);
								break;
							case 0x25://dec h
								Dec8(ref HL.hi);
								break;
							case 0x26://ld h,nn
								LoadImmediate(ref HL.hi);
								break;
							case 0x27://daa
								DecimalAdjustA();
								break;
							case 0x28://jr z,nn
								JumpRelative(IsZero);
								break;
							case 0x29://add hl,hl
								AddHL(HL.w);
								break;
							case 0x2A://ldi a,[hl]
								AF.hi = Read(HL.w);
								HL.w++;
								break;
							case 0x2B://dec hl
								Dec16(ref HL.w);
								break;
							case 0x2C://inc l
								Inc8(ref HL.lo);
								break;
							case 0x2D://dec l
								Dec8(ref HL.lo);
								break;
							case 0x2E://ld l,nn
								LoadImmediate(ref HL.lo);
								break;
							case 0x2F://cpl
								CPL();
								break;
							#endregion
							#region Ops 0x30-0x3F
							case 0x30://jr nc,nn
								JumpRelative(!IsCarry);
								break;
							case 0x31://ld sp,nnnn
								Load16Immediate(ref SP);
								break;
							case 0x32://ldd [hl],a
								LoadHL(AF.hi, LoadHLType.Dec);
								break;
							case 0x33://inc sp
								Inc16(ref SP.w);
								break;
							case 0x34://inc [hl]
								IncHL();
								break;
							case 0x35://dec [hl]
								DecHL();
								break;
							case 0x36://ld [hl],nn
								LoadHL(ReadPC(), LoadHLType.None);
								break;
							case 0x37://scf
								SCF();
								break;
							case 0x38://jr c,nn
								JumpRelative(IsCarry);
								break;
							case 0x39://add hl,sp
								AddHL(SP.w);
								break;
							case 0x3A://ldd a,[hl]
								AF.hi = Read(HL.w);
								HL.w--;
								break;
							case 0x3B://dec sp
								Dec16(ref SP.w);
								break;
							case 0x3C://inc a
								Inc8(ref AF.hi);
								break;
							case 0x3D://dec a
								Dec8(ref AF.hi);
								break;
							case 0x3E://ld a,nn
								LoadImmediate(ref AF.hi);
								break;
							case 0x3F://ccf
								CCF();
								break;
							#endregion
							#region Ops 0x40-0x4F
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
								BC.hi = AF.hi;
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
								BC.lo = AF.hi;
								break;
							#endregion
							#region Ops 0x50-0x5F
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
								DE.hi = AF.hi;
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
								DE.lo = AF.hi;
								break;
							#endregion
							#region Ops 0x60-0x6F
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
								HL.hi = AF.hi;
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
								HL.lo = AF.hi;
								break;
							#endregion
							#region Ops 0x70-0x7F
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
								Halt();
								break;
							case 0x77://ld [hl],a
								Write(HL.w, AF.hi);
								break;
							case 0x78://ld a,b
								AF.hi = BC.hi;
								break;
							case 0x79://ld a,c
								AF.hi = BC.lo;
								break;
							case 0x7A://ld a,d
								AF.hi = DE.hi;
								break;
							case 0x7B://ld a,e
								AF.hi = DE.lo;
								break;
							case 0x7C://ld a,h
								AF.hi = HL.hi;
								break;
							case 0x7D://ld a,l
								AF.hi = HL.lo;
								break;
							case 0x7E://ld a,hl
								AF.hi = Read(HL.w);
								break;
							case 0x7F://ld a,a
								break;
							#endregion
							#region Ops 0x80-0x8F
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
								AddA(AF.hi, false);
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
								AddA(AF.hi, true);
								break;
							#endregion
							#region Ops 0x90-0x9F
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
								SubA(AF.hi, false);
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
								SubA(AF.hi, true);
								break;
							#endregion
							#region Ops 0xA0-0xAF
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
								AndA(AF.hi);
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
								XorA(AF.hi);
								break;
							#endregion
							#region Ops 0xB0-0xBF
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
								OrA(AF.hi);
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
								CpA(AF.hi);
								break;
							#endregion
							#region Ops 0xC0-0xCF
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
							#region Ops 0xD0-0xDF
							case 0xD0://ret nc
								CheckedReturn(!IsCarry);
								break;
							case 0xD1://pop de
								Pop(ref DE.w);
								break;
							case 0xD2://jp nc, nnnn
								Jump(!IsCarry);
								break;
							case 0xD3://--
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
							case 0xDB://--
								break;
							case 0xDC://call c,nnnn
								Call(IsCarry);
								break;
							case 0xDD://--
								break;
							case 0xDE://sbc a,nn
								SubA(ReadPC(), true);
								break;
							case 0xDF://rst $18
								RST(0x18);
								break;
							#endregion
							#region Ops 0xE0-0xEF
							case 0xE0://ld [$ffnn],a
								ushort ldaddress = 0xFF00;
								ldaddress |= ReadPC();
								Write(ldaddress, AF.hi);
								break;
							case 0xE1://pop hl
								Pop(ref HL.w);
								break;
							case 0xE2://ld [c],a
								ushort ldcaddress = 0xFF00;
								ldcaddress |= BC.lo;
								Write(ldcaddress, AF.hi);
								break;
							case 0xE3://--
								break;
							case 0xE4://--
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
								Load16Immediate(ref temp);
								Write(temp.w, AF.hi);
								break;
							case 0xEB://--
								break;
							case 0xEC://--
								break;
							case 0xED://--
								break;
							case 0xEE://xor nn
								XorA(ReadPC());
								break;
							case 0xEF://rst $28
								RST(0x28);
								break;
							#endregion
							#region Ops 0xF0-0xFF
							case 0xF0://ld a,[$ffnn]
								ushort nnAddress = 0xFF00;
								nnAddress |= ReadPC();
								AF.hi = Read(nnAddress);
								break;
							case 0xF1://pop af
								Pop(ref AF.w);
								break;
							case 0xF2://ld a,[c]
								ushort ldrcaddress = 0xFF00;
								ldrcaddress |= BC.lo;
								AF.hi = Read(ldrcaddress);
								break;
							case 0xF3://di
								interruptManager.DisableInterrupts();
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
								LdHLSPN();
								break;
							case 0xF9://ld sp,hl
								SP.w = HL.w;
								CycleCounter += 4;
								break;
							case 0xFA://ld a,[nnnn]
								Register tempLoc = new Register();
								Load16Immediate(ref tempLoc);
								AF.hi = Read(tempLoc.w);
								break;
							case 0xFB://ei
								interruptManager.EnableInterrupts();
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
						break;
				}
				cycles -= CycleCounter;
				mmu.UpdateCounter(CycleCounter);
			}
		}

		public void CBstep()
		{
			switch (ReadPC())
			{
				#region RLC
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
					RLC(ref AF.hi);
					break;
				#endregion
				#region RRC
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
					RRC(ref AF.hi);
					break;
				#endregion
				#region RL
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
					RL(ref AF.hi);
					break;
				#endregion
				#region RR
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
					RR(ref AF.hi);
					break;
				#endregion
				#region SLA
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
					SLA(ref AF.hi);
					break;
				#endregion
				#region SRA
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
					SRA(ref AF.hi);
					break;
				#endregion
				#region Swap
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
					Swap(ref AF.hi);
					break;
				#endregion
				#region SRL
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
					SRL(ref AF.hi);
					break;
				#endregion
				#region Bit
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
					Bit(AF.hi, 0);
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
					Bit(AF.hi, 1);
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
					Bit(AF.hi, 2);
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
					Bit(AF.hi, 3);
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
					Bit(AF.hi, 4);
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
					Bit(AF.hi, 5);
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
					Bit(AF.hi, 6);
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
					Bit(AF.hi, 7);
					break;
				#endregion
				#region Reset
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
					Reset(ref AF.hi, 0);
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
					Reset(ref AF.hi, 1);
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
					Reset(ref AF.hi, 2);
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
					Reset(ref AF.hi, 3);
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
					Reset(ref AF.hi, 4);
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
					Reset(ref AF.hi, 5);
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
					Reset(ref AF.hi, 6);
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
					Reset(ref AF.hi, 7);
					break;
				#endregion
				#region Set
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
					Set(ref AF.hi, 0);
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
					Set(ref AF.hi, 1);
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
					Set(ref AF.hi, 2);
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
					Set(ref AF.hi, 3);
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
					Set(ref AF.hi, 4);
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
					Set(ref AF.hi, 5);
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
					Set(ref AF.hi, 6);
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
					Set(ref AF.hi, 7);
					break;
				#endregion
			}
		}

		private void CheckInterrupts()
		{
			if (!interruptManager.InterruptsEnabled()) return;
			InterruptType iType = interruptManager.FetchNextInterrupt();
			if (iType != InterruptType.None)
			{
				ushort intVector = 0;
				switch (iType)
				{
					case InterruptType.VBlank:
						intVector = IntVector_VBlank;
						break;
					case InterruptType.LCDC:
						intVector = IntVector_LCDC;
						break;
					case InterruptType.Timer:
						intVector = IntVector_Timer;
						break;
					case InterruptType.Serial:
						intVector = IntVector_Serial;
						break;
					case InterruptType.Joypad:
						intVector = IntVector_Joypad;
						break;
				}
				Push(PC.w);
				PCChange(intVector);
				interruptManager.DisableInterrupts();
				state = CPUState.Normal;
			}
		}

		#region Reading and writing
		private byte Read(ushort src)
		{
			CycleCounter += 4;
			return mmu.Read(src);
		}
		private ushort ReadWord(ushort src)
		{
			ushort ret = Read(src);
			src++;
			ret |= (ushort)(Read(src) << 8);
			return ret;
		}
		private ushort ReadPCWord()
		{
			ushort ret = ReadPC();
			ret |= (ushort)(ReadPC() << 8);
			return ret;
		}
		private byte ReadPC()
		{
			byte read = Read(PC.w);
			PC.w++;
			return read;
		}
		private byte ReadSP()
		{
			byte read = Read(SP.w);
			SP.w++;
			return read;
		}
		private void Write(ushort dest, byte data)
		{
			CycleCounter += 4;
			mmu.Write(dest, data);
		}
		private void WriteWord(ushort dest, ushort data)
		{
			Write(dest, (byte)data);
			dest++;
			Write(dest, (byte)(data >> 8));
		}
		private void PCChange(ushort newVal)
		{
			PC.w = newVal;
			CycleCounter += 4;
		}
		#endregion

		#region Instruction Implementation
		#region 8-bit Arithmetic
		private void Inc8(ref byte refreg)
		{
			int temp = refreg + 1;
			int tempover = refreg ^ 1 ^ temp;
			IsHalfCarry = ((tempover & 0x10) != 0);
			IsNegativeOp = false;
			refreg++;
			IsZero = (refreg == 0);
		}
		private void Dec8(ref byte refreg)
		{
			int temp = refreg - 1;
			int tempover = refreg ^ 0xFF ^ temp;
			IsHalfCarry = ((tempover & 0x10) != 0);
			IsNegativeOp = true;
			refreg--;
			IsZero = (refreg == 0);
		}
		private void IncHL()
		{
			byte incHL = Read(HL.w);
			incHL++;
			Write(HL.w, incHL);
		}
		private void DecHL()
		{
			byte decHL = Read(HL.w);
			decHL--;
			Write(HL.w, decHL);
		}
		private void AddA(byte add, bool addCarry)
		{
			byte tempAdd = (byte)(add + (addCarry ? IsCarry ? 1 : 0 : 0));
			int temp = AF.hi + tempAdd;
			int tempover = AF.hi ^ tempAdd ^ temp;
			IsHalfCarry = ((tempover & 0x10) != 0);
			IsCarry = ((tempover & 0x100) != 0);
			IsNegativeOp = false;
			AF.hi += tempAdd;
			IsZero = (AF.hi == 0);
		}
		private void SubA(byte sub, bool subCarry)
		{
			byte tempsub = (byte)(sub + (subCarry ? IsCarry ? 1 : 0 : 0));
			int temp = AF.hi - tempsub;
			int tempover = AF.hi ^ tempsub ^ temp;
			IsHalfCarry = ((tempover & 0x10) != 0);
			IsCarry = ((tempover & 0x100) != 0);
			IsNegativeOp = true;
			AF.hi -= tempsub;
			IsZero = (AF.hi == 0);
		}
		private void AndA(byte val)
		{
			AF.hi &= val;
			IsZero = AF.hi == 0;
			IsNegativeOp = false;
			IsHalfCarry = true;
			IsCarry = false;
		}
		private void OrA(byte val)
		{
			AF.hi |= val;
			IsZero = AF.hi == 0;
			IsNegativeOp = false;
			IsHalfCarry = false;
			IsCarry = false;
		}
		private void XorA(byte val)
		{
			AF.hi ^= val;
			IsZero = AF.hi == 0;
			IsNegativeOp = false;
			IsHalfCarry = false;
			IsCarry = false;
		}
		private void CpA(byte val)
		{
			IsNegativeOp = true;
			int temp = AF.hi - val;
			int tempX = AF.hi ^ val ^ temp;
			IsHalfCarry = (tempX & 0x10) != 0;
			IsCarry = (tempX & 0x100) != 0;
			IsZero = (temp & 0xFF) == 0;
		}
		private void DecimalAdjustA()
		{
			byte correction = (byte)(IsCarry ? 0x60 : 0x00);
			if (IsHalfCarry) correction |= 0x06;
			if (!IsNegativeOp)
			{
				if ((AF.hi & 0x0F) > 0x09) correction |= 0x06;
				if (AF.hi > 0x99) correction |= 0x60;
				AF.hi += correction;
			}
			else
			{
				AF.hi -= correction;
			}
			IsCarry = ((correction << 2) & 0x100) != 0;
			IsZero = AF.hi == 0;
		}
		private void CPL()
		{
			IsNegativeOp = true;
			IsHalfCarry = true;
			AF.hi ^= 0xFF;
		}
		#endregion

		#region 16-bit Arithmetic
		private void Inc16(ref ushort reg)
		{
			reg++;
			CycleCounter += 4;
		}
		private void Dec16(ref ushort reg)
		{
			reg--;
			CycleCounter += 4;
		}
		private void AddHL(ushort refreg)
		{
			int temp = HL.w + refreg;
			int tempover = HL.w ^ refreg ^ temp;
			IsHalfCarry = ((tempover & 0x1000) != 0);
			IsCarry = ((tempover & 0x10000) != 0);
			IsNegativeOp = false;
			HL.w += refreg;
			CycleCounter += 4;
		}
		private void AddSP(byte refreg)
		{
			int temp = SP.w + (sbyte)refreg;
			int tempover = SP.w ^ (sbyte)refreg ^ temp;
			IsHalfCarry = ((tempover & 0x1000) != 0);
			IsCarry = ((tempover & 0x10000) != 0);
			IsNegativeOp = false;
			IsZero = false;
			SP.w = (ushort)temp;
			CycleCounter += 8;
		}
		private void LdHLSPN()
		{
			sbyte offHL = (sbyte)ReadPC();
			int tempAHL = SP.w + offHL;
			int tempAHX = SP.w ^ offHL ^ tempAHL;
			IsHalfCarry = (tempAHX & 0x1000) != 0;
			IsCarry = (tempAHX & 0x10000) != 0;
			IsNegativeOp = false;
			IsZero = false;
			HL.w = (ushort)tempAHL;
			CycleCounter += 4;
		}
		#endregion

		#region Rotate/Shift/Swap
		private void Swap(ref byte val)
		{
			IsNegativeOp = IsHalfCarry = IsCarry = false;
			IsZero = val == 0;
			val = (byte)((val << 4) | (val >> 4));
		}
		private void RL(ref byte val)
		{
			byte carry = (byte)(IsCarry ? 1 : 0);
			IsCarry = (val & 0x80) != 0;
			val = (byte)(val << 1 | carry);
			IsZero = val == 0;
		}
		private void RR(ref byte val)
		{
			byte carry = (byte)(IsCarry ? 0x80 : 0);
			IsCarry = (val & 0x1) != 0;
			val = (byte)(val >> 1 | carry);
			IsZero = val == 0;
		}
		private void RLC(ref byte val)
		{
			IsCarry = (val & 0x80) != 0;
			IsNegativeOp = false;
			IsHalfCarry = false;
			val = (byte)(val << 1 | val >> 7);
			IsZero = val == 0;
		}
		private void RRC(ref byte val)
		{
			IsCarry = (val & 0x1) != 0;
			IsNegativeOp = false;
			IsHalfCarry = false;
			val = (byte)(val >> 1 | val << 7);
			IsZero = (val == 0);
		}
		private void SLA(ref byte val)
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
		}
		private void SRL(ref byte val)
		{
			IsCarry = (val & 0x1) != 0;
			IsNegativeOp = false;
			IsHalfCarry = false;
			val >>= 1;
			IsZero = val == 0;
		}
		#endregion

		#region Bit Operations
		private void Bit(byte val, int bit)
		{
			IsNegativeOp = false;
			IsHalfCarry = true;
			IsZero = (val & (1 << bit)) == 0;
		}
		private void Set(ref byte val, int bit)
		{
			byte[] setBits = new byte[8]
			{
				1, 2, 4, 8, 16, 32, 64, 128
			};
			val |= setBits[bit];
		}
		private void Reset(ref byte val, int bit)
		{
			byte[] resetBits = new byte[8]
			{
				0xFE, 0xFD, 0xFB, 0xF7, 0xEF, 0xDF, 0xBF, 0x7F
			};
			val &= resetBits[bit];
		}
		#endregion

		#region Jump/Call/Return
		private void Jump(bool isCondTrue)
		{
			Register jl = new Register();
			jl.lo = ReadPC();
			jl.hi = ReadPC();
			if (isCondTrue)
			{
				PCChange(jl.w);
			}
		}
		private void JumpRelative(bool isCondTrue)
		{
			sbyte off = (sbyte)ReadPC();
			if (isCondTrue)
			{
				PCChange((ushort)(PC.w + off));
			}
		}
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
		}
		private void Return(bool enableInterrupts)
		{
			ushort retAddr = 0;
			Pop(ref retAddr);
			PCChange(retAddr);
			state = CPUState.Normal;
			if (enableInterrupts) interruptManager.EnableInterrupts();
		}
		private void CheckedReturn(bool isCondTrue)
		{
			CycleCounter += 4;
			if (isCondTrue)
			{
				ushort retAddr = 0;
				Pop(ref retAddr);
				PCChange(retAddr);
			}
		}
		private void RST(byte jumpVector)
		{
			Push(PC.w);
			PCChange((ushort)(jumpVector & 0x38));
		}
		#endregion

		#region CPU Commands
		private void Halt()
		{
			if (interruptManager.InterruptsEnabled())
			{
				state = CPUState.Halt;
			}
			else if (interruptManager.InterruptsReady)
			{
				//if (mmu.IsCGB) CycleCounter += 4;
				/*else*/
				RepeatLastInstruction = true;
			}
		}
		private void Stop()
		{
			//state = CPUState.Stop;
			PC.w++;
		}
		private void DI()
		{
			interruptManager.DisableInterrupts();
		}
		private void EI()
		{
			interruptManager.EnableInterrupts();
		}
		private void CCF()
		{
			IsCarry = !IsCarry;
			IsHalfCarry = false;
			IsNegativeOp = false;
		}
		private void SCF()
		{
			IsCarry = true;
			IsHalfCarry = false;
			IsNegativeOp = false;
		}
		#endregion

		#region Stack Commands
		private void Push(ushort pushData)
		{
			Write(SP.w, (byte)(pushData >> 8));
			SP.w--;
			Write(SP.w, (byte)pushData);
			SP.w--;
		}
		private void Pop(ref ushort popReg)
		{
			ushort x = ReadSP();
			x |= (ushort)(ReadSP() << 8);
			popReg = x;
		}
		#endregion

		#region Load Commands
		private void LoadImmediate(ref byte reg)
		{
			reg = ReadPC();
		}
		private void LoadFromMemory(ref byte reg, ushort dest)
		{
			reg = Read(dest);
		}
		private void Load16Immediate(ref Register reg)
		{
			reg.lo = ReadPC();
			reg.hi = ReadPC();
		}
		private void LoadHL(byte val, LoadHLType type)
		{
			Write(HL.w, val);
			if (type == LoadHLType.Inc) HL.w++;
			else if (type == LoadHLType.Dec) HL.w--;
		}
		private enum LoadHLType { None, Inc, Dec }
		#endregion
		#endregion
	}
}
