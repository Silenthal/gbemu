using GBEmu.Emulator.IO;

namespace GBEmu.Emulator
{
    /// <summary>
    /// Reads a byte from the MMU.
    /// </summary>
    /// <param name="address">The address to read from.</param>
    /// <returns>The contents of the memory location.</returns>
    public delegate byte ReadFromMMUDelegate(int address);

    /// <summary>
    /// Writes a byte to the MMU.
    /// </summary>
    /// <param name="address">The address to write to.</param>
    /// <param name="data">The data to write.</param>
    public delegate void WriteToMMUDelegate(int address, byte data);

    /// <summary>
    /// Updates the system time to reflect the cycles passed.
    /// </summary>
    /// <param name="cycles">The amount of time that has passed, in cycles.</param>
    public delegate void UpdateTimeDelegate(int cycles);

    /// <summary>
    /// Represents a Sharp LR35902 CPU.
    /// </summary>
    public class CPU
    {
        #region Private Members

        private CPUState state;

        private InterruptManager interruptManager;

        /// <summary>
        /// A flag that indicates, in DMG mode, whether the HALT bug has taken place and the next instruction should be skipped.
        /// </summary>
        private bool RepeatLastInstruction;

        private ReadFromMMUDelegate ReadGB;

        private WriteToMMUDelegate WriteGB;

        private UpdateTimeDelegate UpdateTimeGB;

        #region Interrupt Vectors

        private const ushort IntVector_VBlank = 0x40;
        private const ushort IntVector_LCDC = 0x48;
        private const ushort IntVector_Timer = 0x50;
        private const ushort IntVector_Serial = 0x58;
        private const ushort IntVector_Joypad = 0x60;

        #endregion Interrupt Vectors

        #region Flag Properties

        /// <summary>
        /// This flag specifies whether the last operation resulted in a 0.
        /// </summary>
        private bool IsZero
        {
            get
            {
                return (AF.lo & 0x80) != 0;
            }
            set
            {
                if (value)
                    AF.lo |= 0x80;
                else
                    AF.lo &= 0x7F;
            }
        }

        /// <summary>
        /// This flag specifies whether the last operation was a subtract operation.
        /// </summary>
        private bool IsNegativeOp
        {
            get
            {
                return (AF.lo & 0x40) != 0;
            }
            set
            {
                if (value)
                    AF.lo |= 0x40;
                else
                    AF.lo &= 0xBF;
            }
        }

        /// <summary>
        /// This flag specifies whether the last operation resulted in carry between two nibbles of a byte.
        /// </summary>
        private bool IsHalfCarry
        {
            get
            {
                return (AF.lo & 0x20) != 0;
            }
            set
            {
                if (value)
                    AF.lo |= 0x20;
                else
                    AF.lo &= 0xDF;
            }
        }

        /// <summary>
        /// This flag specifies whether the last operation resulted in carry from the top nibble of a byte.
        /// </summary>
        private bool IsCarry
        {
            get
            {
                return (AF.lo & 0x10) != 0;
            }
            set
            {
                if (value)
                    AF.lo |= 0x10;
                else
                    AF.lo &= 0xEF;
            }
        }

        #endregion Flag Properties

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

        #endregion Set

        #region Reset

        private const byte RESET_7 = 0x7F;
        private const byte RESET_6 = 0xBF;
        private const byte RESET_5 = 0xDF;
        private const byte RESET_4 = 0xEF;
        private const byte RESET_3 = 0xF7;
        private const byte RESET_2 = 0xFB;
        private const byte RESET_1 = 0xFD;
        private const byte RESET_0 = 0xFE;

        #endregion Reset

        #endregion Bit Constants

        #region Registers

        private RegisterPair AF;
        private RegisterPair BC;
        private RegisterPair DE;
        private RegisterPair HL;
        private RegisterPair PC;
        private RegisterPair SP;

        #endregion Registers

        /// <summary>
        /// Contains the number of cycles the last instruction ran for.
        /// </summary>
        private int CyclesSinceLastStep;

        #endregion Private Members

        public CPU(InterruptManager iM, ReadFromMMUDelegate mmuRead, WriteToMMUDelegate mmuWrite, UpdateTimeDelegate sysTimeUpdate)
        {
            interruptManager = iM;
            ReadGB = mmuRead;
            WriteGB = mmuWrite;
            UpdateTimeGB = sysTimeUpdate;
            InitializeDefaultValues();
        }

        /// <summary>
        /// Initializes the default values for a standard DMG CPU.
        /// </summary>
        public void InitializeDefaultValues()
        {
            PC.w = 0x0100;
            SP.w = 0xFFFE;

            #region DMG-specific register variables

            // DMGABC
            AF.w = 0x01B0;
            BC.w = 0x0013;
            DE.w = 0x00D8;
            HL.w = 0x014D;

            //// DMG0
            //AF.w = 0x0100;
            //BC.w = 0xFF13;
            //DE.w = 0x00C1;
            //HL.w = 0x8403;

            //// MGB
            //AF.w = 0xFFB0;
            //BC.w = 0x0013;
            //DE.w = 0x00D8;
            //HL.w = 0x014D;

            //// SGB
            //AF.w = 0x0100;
            //BC.w = 0x0014;
            //DE.w = 0x0000;
            //HL.w = 0xC060;

            //// SGB2
            //AF.w = 0xFF00;
            //BC.w = 0x0014;
            //DE.w = 0x0000;
            //HL.w = 0xC060;

            #endregion DMG-specific register variables

            CyclesSinceLastStep = 0;
            state = CPUState.Normal;
            RepeatLastInstruction = false;
        }

        #region CPU Actions

        /// <summary>
        /// Runs the CPU for the specified amount of cycles.
        /// </summary>
        /// <param name="cycles">The amount of cycles to run.</param>
        public void RunFor(int cycles)
        {
            while (cycles > 0)
            {
                Step();
                cycles -= CyclesSinceLastStep;
            }
        }

        /// <summary>
        /// Runs the CPU for the duration of one frame (70224 LCD cycles).
        /// </summary>
        public void RunFrame()
        {
            RunFor(70224);
        }

        /// <summary>
        /// Fetches and executes a single instruction.
        /// </summary>
        public void Step()
        {
            CyclesSinceLastStep = 0;
            CheckInterrupts();
            switch (state)
            {
                case CPUState.Halt:
                    UpdateSystemTime(4);
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
                            BC.w = ReadPCWord();
                            break;

                        case 0x02://ld [bc],a
                            WriteMMU(BC.w, AF.hi);
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
                            BC.hi = ReadPC();
                            break;

                        case 0x07://rlca
                            RLCA();
                            break;

                        case 0x08://ld [nnnn],sp
                            WriteWord(ReadPCWord(), SP.w);
                            break;

                        case 0x09://add hl,bc
                            AddHL(BC.w);
                            break;

                        case 0x0A://ld a,[bc]
                            AF.hi = ReadMMU(BC.w);
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
                            BC.lo = ReadPC();
                            break;

                        case 0x0F://rrca
                            RRCA();
                            break;

                        #endregion Ops 0x00-0x0F

                        #region Ops 0x10-0x1F

                        case 0x10://stop
                            Stop();
                            break;

                        case 0x11://ld de,nnnn
                            DE.w = ReadPCWord();
                            break;

                        case 0x12://ld [de],a
                            WriteMMU(DE.w, AF.hi);
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
                            DE.hi = ReadPC();
                            break;

                        case 0x17://rla
                            RLA();
                            break;

                        case 0x18://jr nn
                            JumpRelative(true);
                            break;

                        case 0x19://add hl,de
                            AddHL(DE.w);
                            break;

                        case 0x1A://ld a,[de]
                            AF.hi = ReadMMU(DE.w);
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
                            DE.lo = ReadPC();
                            break;

                        case 0x1F://rra
                            RRA();
                            break;

                        #endregion Ops 0x10-0x1F

                        #region Ops 0x20-0x2F

                        case 0x20://jr nz,nn
                            JumpRelative(!IsZero);
                            break;

                        case 0x21://ld hl,nnnn
                            HL.w = ReadPCWord();
                            break;

                        case 0x22://ldi [hl],a
                            WriteMMU(HL.w, AF.hi);
                            HL.w++;
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
                            HL.hi = ReadPC();
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
                            AF.hi = ReadMMU(HL.w);
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
                            HL.lo = ReadPC();
                            break;

                        case 0x2F://cpl
                            CPL();
                            break;

                        #endregion Ops 0x20-0x2F

                        #region Ops 0x30-0x3F

                        case 0x30://jr nc,nn
                            JumpRelative(!IsCarry);
                            break;

                        case 0x31://ld sp,nnnn
                            SP.w = ReadPCWord();
                            break;

                        case 0x32://ldd [hl],a
                            WriteMMU(HL.w, AF.hi);
                            HL.w--;
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
                            WriteMMU(HL.w, ReadPC());
                            break;

                        case 0x37://scf
                            SetCarryFlag();
                            break;

                        case 0x38://jr c,nn
                            JumpRelative(IsCarry);
                            break;

                        case 0x39://add hl,sp
                            AddHL(SP.w);
                            break;

                        case 0x3A://ldd a,[hl]
                            AF.hi = ReadMMU(HL.w);
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
                            AF.hi = ReadPC();
                            break;

                        case 0x3F://ccf
                            ComplementCarryFlag();
                            break;

                        #endregion Ops 0x30-0x3F

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
                            BC.hi = ReadMMU(HL.w);
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
                            BC.lo = ReadMMU(HL.w);
                            break;

                        case 0x4F://ld c,a
                            BC.lo = AF.hi;
                            break;

                        #endregion Ops 0x40-0x4F

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
                            DE.hi = ReadMMU(HL.w);
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
                            DE.lo = ReadMMU(HL.w);
                            break;

                        case 0x5F://ld e,a
                            DE.lo = AF.hi;
                            break;

                        #endregion Ops 0x50-0x5F

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
                            HL.hi = ReadMMU(HL.w);
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
                            HL.lo = ReadMMU(HL.w);
                            break;

                        case 0x6F://ld l,a
                            HL.lo = AF.hi;
                            break;

                        #endregion Ops 0x60-0x6F

                        #region Ops 0x70-0x7F

                        case 0x70://ld [hl],b
                            WriteMMU(HL.w, BC.hi);
                            break;

                        case 0x71://ld [hl],c
                            WriteMMU(HL.w, BC.lo);
                            break;

                        case 0x72://ld [hl],d
                            WriteMMU(HL.w, DE.hi);
                            break;

                        case 0x73://ld [hl],e
                            WriteMMU(HL.w, DE.lo);
                            break;

                        case 0x74://ld [hl],h
                            WriteMMU(HL.w, HL.hi);
                            break;

                        case 0x75://ld [hl],l
                            WriteMMU(HL.w, HL.lo);
                            break;

                        case 0x76://halt
                            Halt();
                            break;

                        case 0x77://ld [hl],a
                            WriteMMU(HL.w, AF.hi);
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
                            AF.hi = ReadMMU(HL.w);
                            break;

                        case 0x7F://ld a,a
                            break;

                        #endregion Ops 0x70-0x7F

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
                            AddA(ReadMMU(HL.w), false);
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
                            AddA(ReadMMU(HL.w), true);
                            break;

                        case 0x8F://adc a,a
                            AddA(AF.hi, true);
                            break;

                        #endregion Ops 0x80-0x8F

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
                            SubA(ReadMMU(HL.w), false);
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
                            SubA(ReadMMU(HL.w), true);
                            break;

                        case 0x9F://sbc a,a
                            SubA(AF.hi, true);
                            break;

                        #endregion Ops 0x90-0x9F

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
                            AndA(ReadMMU(HL.w));
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
                            XorA(ReadMMU(HL.w));
                            break;

                        case 0xAF://xor a,a
                            XorA(AF.hi);
                            break;

                        #endregion Ops 0xA0-0xAF

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
                            OrA(ReadMMU(HL.w));
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
                            CpA(ReadMMU(HL.w));
                            break;

                        case 0xBF://cp a,a
                            CpA(AF.hi);
                            break;

                        #endregion Ops 0xB0-0xBF

                        #region Ops 0xC0-0xCF

                        case 0xC0://ret nz
                            CheckedReturn(!IsZero);
                            break;

                        case 0xC1://pop bc
                            Pop(ref BC.w);
                            break;

                        case 0xC2://jp nz,nnnn
                            JumpImmediate(!IsZero);
                            break;

                        case 0xC3://jp nnnn
                            JumpImmediate(true);
                            break;

                        case 0xC4://call nz,nnnn
                            Call(!IsZero);
                            break;

                        case 0xC5://push bc
                            PushRegister(BC.w);
                            break;

                        case 0xC6://add a,nn
                            AddA(ReadPC(), false);
                            break;

                        case 0xC7://rst $00
                            Reset(0x00);
                            break;

                        case 0xC8://ret z
                            CheckedReturn(IsZero);
                            break;

                        case 0xC9://ret
                            Return(false);
                            break;

                        case 0xCA://jp z,nnnn
                            JumpImmediate(IsZero);
                            break;

                        case 0xCB://CB Instruction
                            StepCB();
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
                            Reset(0x08);
                            break;

                        #endregion Ops 0xC0-0xCF

                        #region Ops 0xD0-0xDF

                        case 0xD0://ret nc
                            CheckedReturn(!IsCarry);
                            break;

                        case 0xD1://pop de
                            Pop(ref DE.w);
                            break;

                        case 0xD2://jp nc, nnnn
                            JumpImmediate(!IsCarry);
                            break;

                        case 0xD3://--
                            break;

                        case 0xD4://call nc, nnnn
                            Call(!IsCarry);
                            break;

                        case 0xD5://push de
                            PushRegister(DE.w);
                            break;

                        case 0xD6://sub nn
                            SubA(ReadPC(), false);
                            break;

                        case 0xD7://rst $10
                            Reset(0x10);
                            break;

                        case 0xD8://ret c
                            CheckedReturn(IsCarry);
                            break;

                        case 0xD9://reti
                            Return(true);
                            break;

                        case 0xDA://jp c,nnnn
                            JumpImmediate(IsCarry);
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
                            Reset(0x18);
                            break;

                        #endregion Ops 0xD0-0xDF

                        #region Ops 0xE0-0xEF

                        case 0xE0://ld [$ffnn],a
                            WritePort(ReadPC(), AF.hi);
                            break;

                        case 0xE1://pop hl
                            Pop(ref HL.w);
                            break;

                        case 0xE2://ld [c],a
                            WritePort(BC.lo, AF.hi);
                            break;

                        case 0xE3://--
                            break;

                        case 0xE4://--
                            break;

                        case 0xE5://push hl
                            PushRegister(HL.w);
                            break;

                        case 0xE6://and a,nn
                            AndA(ReadPC());
                            break;

                        case 0xE7://rst $20
                            Reset(0x20);
                            break;

                        case 0xE8://add sp,nn
                            SP.w = AddSPImmediate();
                            UpdateSystemTime(4);
                            break;

                        case 0xE9://jp hl
                            PC.w = HL.w;
                            break;

                        case 0xEA://ld [$nnnn],a
                            WriteMMU(ReadPCWord(), AF.hi);
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
                            Reset(0x28);
                            break;

                        #endregion Ops 0xE0-0xEF

                        #region Ops 0xF0-0xFF

                        case 0xF0://ld a,[$ffnn]
                            AF.hi = ReadPort(ReadPC());
                            break;

                        case 0xF1://pop af
                            Pop(ref AF.w);
                            AF.lo &= 0xF0;//Only writes to higher 4 bits of F are possible.
                            break;

                        case 0xF2://ld a,[c]
                            AF.hi = ReadPort(BC.lo);
                            break;

                        case 0xF3://di
                            DisableInterrupts();
                            break;

                        case 0xF4://--
                            break;

                        case 0xF5://push af
                            PushRegister(AF.w);
                            break;

                        case 0xF6://or nn
                            OrA(ReadPC());
                            break;

                        case 0xF7://rst $30
                            Reset(0x30);
                            break;

                        case 0xF8://ldhl sp,nn
                            LdHLSPN();
                            break;

                        case 0xF9://ld sp,hl
                            SP.w = HL.w;
                            UpdateSystemTime(4);
                            break;

                        case 0xFA://ld a,[nnnn]
                            AF.hi = ReadMMU(ReadPCWord());
                            break;

                        case 0xFB://ei
                            EnableInterrupts();
                            break;

                        case 0xFC://--
                            break;

                        case 0xFD://--
                            break;

                        case 0xFE://cp a,nn
                            CpA(ReadPC());
                            break;

                        case 0xFF://rst $38
                            Reset(0x38);
                            break;

                            #endregion Ops 0xF0-0xFF
                    }
                    break;
            }
        }

        /// <summary>
        /// Executes an instruction following a read of 0xCB from the PC.
        /// </summary>
        private void StepCB()
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
                    byte rlchl = ReadMMU(HL.w);
                    RLC(ref rlchl);
                    WriteMMU(HL.w, rlchl);
                    break;

                case 0x07://rlc a
                    RLC(ref AF.hi);
                    break;

                #endregion RLC

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
                    byte rrchl = ReadMMU(HL.w);
                    RRC(ref rrchl);
                    WriteMMU(HL.w, rrchl);
                    break;

                case 0x0F://rrc a
                    RRC(ref AF.hi);
                    break;

                #endregion RRC

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
                    byte rlhl = ReadMMU(HL.w);
                    RL(ref rlhl);
                    WriteMMU(HL.w, rlhl);
                    break;

                case 0x17://rl a
                    RL(ref AF.hi);
                    break;

                #endregion RL

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
                    byte rrhl = ReadMMU(HL.w);
                    RR(ref rrhl);
                    WriteMMU(HL.w, rrhl);
                    break;

                case 0x1F://rr a
                    RR(ref AF.hi);
                    break;

                #endregion RR

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
                    byte slahl = ReadMMU(HL.w);
                    SLA(ref slahl);
                    WriteMMU(HL.w, slahl);
                    break;

                case 0x27://sla a
                    SLA(ref AF.hi);
                    break;

                #endregion SLA

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
                    byte srahl = ReadMMU(HL.w);
                    SRA(ref srahl);
                    WriteMMU(HL.w, srahl);
                    break;

                case 0x2F://sra a
                    SRA(ref AF.hi);
                    break;

                #endregion SRA

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
                    byte swaphl = ReadMMU(HL.w);
                    Swap(ref swaphl);
                    WriteMMU(HL.w, swaphl);
                    break;

                case 0x37://swap a
                    Swap(ref AF.hi);
                    break;

                #endregion Swap

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
                    byte srlHL = ReadMMU(HL.w);
                    SRL(ref srlHL);
                    WriteMMU(HL.w, srlHL);
                    break;

                case 0x3F://srl a
                    SRL(ref AF.hi);
                    break;

                #endregion SRL

                #region Bit

                case 0x40://bit 0, b
                    TestBit(BC.hi, 0);
                    break;

                case 0x41://bit 0, c
                    TestBit(BC.lo, 0);
                    break;

                case 0x42://bit 0, d
                    TestBit(DE.hi, 0);
                    break;

                case 0x43://bit 0, e
                    TestBit(DE.lo, 0);
                    break;

                case 0x44://bit 0, h
                    TestBit(HL.hi, 0);
                    break;

                case 0x45://bit 0, l
                    TestBit(HL.lo, 0);
                    break;

                case 0x46://bit 0, [hl]
                    TestBit(ReadMMU(HL.w), 0);
                    break;

                case 0x47://bit 0, a
                    TestBit(AF.hi, 0);
                    break;

                case 0x48://bit 1, b
                    TestBit(BC.hi, 1);
                    break;

                case 0x49://bit 1, c
                    TestBit(BC.lo, 1);
                    break;

                case 0x4A://bit 1, d
                    TestBit(DE.hi, 1);
                    break;

                case 0x4B://bit 1, e
                    TestBit(DE.lo, 1);
                    break;

                case 0x4C://bit 1, h
                    TestBit(HL.hi, 1);
                    break;

                case 0x4D://bit 1, l
                    TestBit(HL.lo, 1);
                    break;

                case 0x4E://bit 1, hl
                    TestBit(ReadMMU(HL.w), 1);
                    break;

                case 0x4F://bit 1, a
                    TestBit(AF.hi, 1);
                    break;

                case 0x50://bit 2, b
                    TestBit(BC.hi, 2);
                    break;

                case 0x51://bit 2, c
                    TestBit(BC.lo, 2);
                    break;

                case 0x52://bit 2, d
                    TestBit(DE.hi, 2);
                    break;

                case 0x53://bit 2, e
                    TestBit(DE.lo, 2);
                    break;

                case 0x54://bit 2, h
                    TestBit(HL.hi, 2);
                    break;

                case 0x55://bit 2, l
                    TestBit(HL.lo, 2);
                    break;

                case 0x56://bit 2, hl
                    TestBit(ReadMMU(HL.w), 2);
                    break;

                case 0x57://bit 2, a
                    TestBit(AF.hi, 2);
                    break;

                case 0x58://bit 3, b
                    TestBit(BC.hi, 3);
                    break;

                case 0x59://bit 3, c
                    TestBit(BC.lo, 3);
                    break;

                case 0x5A://bit 3, d
                    TestBit(DE.hi, 3);
                    break;

                case 0x5B://bit 3, e
                    TestBit(DE.lo, 3);
                    break;

                case 0x5C://bit 3, h
                    TestBit(HL.hi, 3);
                    break;

                case 0x5D://bit 3, l
                    TestBit(HL.lo, 3);
                    break;

                case 0x5E://bit 3, hl
                    TestBit(ReadMMU(HL.w), 3);
                    break;

                case 0x5F://bit 3, a
                    TestBit(AF.hi, 3);
                    break;

                case 0x60://bit 4, b
                    TestBit(BC.hi, 4);
                    break;

                case 0x61://bit 4, c
                    TestBit(BC.lo, 4);
                    break;

                case 0x62://bit 4, d
                    TestBit(DE.hi, 4);
                    break;

                case 0x63://bit 4, e
                    TestBit(DE.lo, 4);
                    break;

                case 0x64://bit 4, h
                    TestBit(HL.hi, 4);
                    break;

                case 0x65://bit 4, l
                    TestBit(HL.lo, 4);
                    break;

                case 0x66://bit 4, hl
                    TestBit(ReadMMU(HL.w), 4);
                    break;

                case 0x67://bit 4, a
                    TestBit(AF.hi, 4);
                    break;

                case 0x68://bit 5, b
                    TestBit(BC.hi, 5);
                    break;

                case 0x69://bit 5, c
                    TestBit(BC.lo, 5);
                    break;

                case 0x6A://bit 5, d
                    TestBit(DE.hi, 5);
                    break;

                case 0x6B://bit 5, e
                    TestBit(DE.lo, 5);
                    break;

                case 0x6C://bit 5, h
                    TestBit(HL.hi, 5);
                    break;

                case 0x6D://bit 5, l
                    TestBit(HL.lo, 5);
                    break;

                case 0x6E://bit 5, hl
                    TestBit(ReadMMU(HL.w), 5);
                    break;

                case 0x6F://bit 5, a
                    TestBit(AF.hi, 5);
                    break;

                case 0x70://bit 6, b
                    TestBit(BC.hi, 6);
                    break;

                case 0x71://bit 6, c
                    TestBit(BC.lo, 6);
                    break;

                case 0x72://bit 6, d
                    TestBit(DE.hi, 6);
                    break;

                case 0x73://bit 6, e
                    TestBit(DE.lo, 6);
                    break;

                case 0x74://bit 6, h
                    TestBit(HL.hi, 6);
                    break;

                case 0x75://bit 6, l
                    TestBit(HL.lo, 6);
                    break;

                case 0x76://bit 6, hl
                    TestBit(ReadMMU(HL.w), 6);
                    break;

                case 0x77://bit 6, a
                    TestBit(AF.hi, 6);
                    break;

                case 0x78://bit 7, b
                    TestBit(BC.hi, 7);
                    break;

                case 0x79://bit 7, c
                    TestBit(BC.lo, 7);
                    break;

                case 0x7A://bit 7, d
                    TestBit(DE.hi, 7);
                    break;

                case 0x7B://bit 7, e
                    TestBit(DE.lo, 7);
                    break;

                case 0x7C://bit 7, h
                    TestBit(HL.hi, 7);
                    break;

                case 0x7D://bit 7, l
                    TestBit(HL.lo, 7);
                    break;

                case 0x7E://bit 7, [hl]
                    TestBit(ReadMMU(HL.w), 7);
                    break;

                case 0x7F://bit 7, a
                    TestBit(AF.hi, 7);
                    break;

                #endregion Bit

                #region Reset

                case 0x80://res 0, b
                    ResetBit(ref BC.hi, 0);
                    break;

                case 0x81://res 0, c
                    ResetBit(ref BC.lo, 0);
                    break;

                case 0x82://res 0, d
                    ResetBit(ref DE.hi, 0);
                    break;

                case 0x83://res 0, e
                    ResetBit(ref DE.lo, 0);
                    break;

                case 0x84://res 0, h
                    ResetBit(ref HL.hi, 0);
                    break;

                case 0x85://res 0, l
                    ResetBit(ref HL.lo, 0);
                    break;

                case 0x86://res 0, [hl]
                    WriteMMU(HL.w, (byte)(ReadMMU(HL.w) & RESET_0));
                    break;

                case 0x87://res 0, a
                    ResetBit(ref AF.hi, 0);
                    break;

                case 0x88://res 1, b
                    ResetBit(ref BC.hi, 1);
                    break;

                case 0x89://res 1, c
                    ResetBit(ref BC.lo, 1);
                    break;

                case 0x8A://res 1, d
                    ResetBit(ref DE.hi, 1);
                    break;

                case 0x8B://res 1, e
                    ResetBit(ref DE.lo, 1);
                    break;

                case 0x8C://res 1, h
                    ResetBit(ref HL.hi, 1);
                    break;

                case 0x8D://res 1, l
                    ResetBit(ref HL.lo, 1);
                    break;

                case 0x8E://res 1, [hl]
                    WriteMMU(HL.w, (byte)(ReadMMU(HL.w) & RESET_1));
                    break;

                case 0x8F://res 1, a
                    ResetBit(ref AF.hi, 1);
                    break;

                case 0x90://res 2, b
                    ResetBit(ref BC.hi, 2);
                    break;

                case 0x91://res 2, c
                    ResetBit(ref BC.lo, 2);
                    break;

                case 0x92://res 2, d
                    ResetBit(ref DE.hi, 2);
                    break;

                case 0x93://res 2, e
                    ResetBit(ref DE.lo, 2);
                    break;

                case 0x94://res 2, h
                    ResetBit(ref HL.hi, 2);
                    break;

                case 0x95://res 2, l
                    ResetBit(ref HL.lo, 2);
                    break;

                case 0x96://res 2, [hl]
                    WriteMMU(HL.w, (byte)(ReadMMU(HL.w) & RESET_2));
                    break;

                case 0x97://res 2, a
                    ResetBit(ref AF.hi, 2);
                    break;

                case 0x98://res 3, b
                    ResetBit(ref BC.hi, 3);
                    break;

                case 0x99://res 3, c
                    ResetBit(ref BC.lo, 3);
                    break;

                case 0x9A://res 3, d
                    ResetBit(ref DE.hi, 3);
                    break;

                case 0x9B://res 3, e
                    ResetBit(ref DE.lo, 3);
                    break;

                case 0x9C://res 3, h
                    ResetBit(ref HL.hi, 3);
                    break;

                case 0x9D://res 3, l
                    ResetBit(ref HL.lo, 3);
                    break;

                case 0x9E://res 3, [hl]
                    WriteMMU(HL.w, (byte)(ReadMMU(HL.w) & RESET_3));
                    break;

                case 0x9F://res 3, a
                    ResetBit(ref AF.hi, 3);
                    break;

                case 0xA0://res 4, b
                    ResetBit(ref BC.hi, 4);
                    break;

                case 0xA1://res 4, c
                    ResetBit(ref BC.lo, 4);
                    break;

                case 0xA2://res 4, d
                    ResetBit(ref DE.hi, 4);
                    break;

                case 0xA3://res 4, e
                    ResetBit(ref DE.lo, 4);
                    break;

                case 0xA4://res 4, h
                    ResetBit(ref HL.hi, 4);
                    break;

                case 0xA5://res 4, l
                    ResetBit(ref HL.lo, 4);
                    break;

                case 0xA6://res 4, [hl]
                    WriteMMU(HL.w, (byte)(ReadMMU(HL.w) & RESET_4));
                    break;

                case 0xA7://res 4, a
                    ResetBit(ref AF.hi, 4);
                    break;

                case 0xA8://res 5, b
                    ResetBit(ref BC.hi, 5);
                    break;

                case 0xA9://res 5, c
                    ResetBit(ref BC.lo, 5);
                    break;

                case 0xAA://res 5, d
                    ResetBit(ref DE.hi, 5);
                    break;

                case 0xAB://res 5, e
                    ResetBit(ref DE.lo, 5);
                    break;

                case 0xAC://res 5, h
                    ResetBit(ref HL.hi, 5);
                    break;

                case 0xAD://res 5, l
                    ResetBit(ref HL.lo, 5);
                    break;

                case 0xAE://res 5, [hl]
                    WriteMMU(HL.w, (byte)(ReadMMU(HL.w) & RESET_5));
                    break;

                case 0xAF://res 5, a
                    ResetBit(ref AF.hi, 5);
                    break;

                case 0xB0://res 6, b
                    ResetBit(ref BC.hi, 6);
                    break;

                case 0xB1://res 6, c
                    ResetBit(ref BC.lo, 6);
                    break;

                case 0xB2://res 6, d
                    ResetBit(ref DE.hi, 6);
                    break;

                case 0xB3://res 6, e
                    ResetBit(ref DE.lo, 6);
                    break;

                case 0xB4://res 6, h
                    ResetBit(ref HL.hi, 6);
                    break;

                case 0xB5://res 6, l
                    ResetBit(ref HL.lo, 6);
                    break;

                case 0xB6://res 6, [hl]
                    WriteMMU(HL.w, (byte)(ReadMMU(HL.w) & RESET_6));
                    break;

                case 0xB7://res 6, a
                    ResetBit(ref AF.hi, 6);
                    break;

                case 0xB8://res 7, b
                    ResetBit(ref BC.hi, 7);
                    break;

                case 0xB9://res 7, c
                    ResetBit(ref BC.lo, 7);
                    break;

                case 0xBA://res 7, d
                    ResetBit(ref DE.hi, 7);
                    break;

                case 0xBB://res 7, e
                    ResetBit(ref DE.lo, 7);
                    break;

                case 0xBC://res 7, h
                    ResetBit(ref HL.hi, 7);
                    break;

                case 0xBD://res 7, l
                    ResetBit(ref HL.lo, 7);
                    break;

                case 0xBE://res 7, [hl]
                    WriteMMU(HL.w, (byte)(ReadMMU(HL.w) & RESET_7));
                    break;

                case 0xBF://res 7, a
                    ResetBit(ref AF.hi, 7);
                    break;

                #endregion Reset

                #region Set

                case 0xC0://set 0, b
                    SetBit(ref BC.hi, 0);
                    break;

                case 0xC1://set 0, c
                    SetBit(ref BC.lo, 0);
                    break;

                case 0xC2://set 0, d
                    SetBit(ref DE.hi, 0);
                    break;

                case 0xC3://set 0, e
                    SetBit(ref DE.lo, 0);
                    break;

                case 0xC4://set 0, h
                    SetBit(ref HL.hi, 0);
                    break;

                case 0xC5://set 0, l
                    SetBit(ref HL.lo, 0);
                    break;

                case 0xC6://set 0, [hl]
                    WriteMMU(HL.w, (byte)(ReadMMU(HL.w) | SET_0));
                    break;

                case 0xC7://set 0, a
                    SetBit(ref AF.hi, 0);
                    break;

                case 0xC8://set 1, b
                    SetBit(ref BC.hi, 1);
                    break;

                case 0xC9://set 1, c
                    SetBit(ref BC.lo, 1);
                    break;

                case 0xCA://set 1, d
                    SetBit(ref DE.hi, 1);
                    break;

                case 0xCB://set 1, e
                    SetBit(ref DE.lo, 1);
                    break;

                case 0xCC://set 1, h
                    SetBit(ref HL.hi, 1);
                    break;

                case 0xCD://set 1, l
                    SetBit(ref HL.lo, 1);
                    break;

                case 0xCE://set 1, [hl]
                    WriteMMU(HL.w, (byte)(ReadMMU(HL.w) | SET_1));
                    break;

                case 0xCF://set 1, a
                    SetBit(ref AF.hi, 1);
                    break;

                case 0xD0://set 2, b
                    SetBit(ref BC.hi, 2);
                    break;

                case 0xD1://set 2, c
                    SetBit(ref BC.lo, 2);
                    break;

                case 0xD2://set 2, d
                    SetBit(ref DE.hi, 2);
                    break;

                case 0xD3://set 2, e
                    SetBit(ref DE.lo, 2);
                    break;

                case 0xD4://set 2, h
                    SetBit(ref HL.hi, 2);
                    break;

                case 0xD5://set 2, l
                    SetBit(ref HL.lo, 2);
                    break;

                case 0xD6://set 2, [hl]
                    WriteMMU(HL.w, (byte)(ReadMMU(HL.w) | SET_2));
                    break;

                case 0xD7://set 2, a
                    SetBit(ref AF.hi, 2);
                    break;

                case 0xD8://set 3, b
                    SetBit(ref BC.hi, 3);
                    break;

                case 0xD9://set 3, c
                    SetBit(ref BC.lo, 3);
                    break;

                case 0xDA://set 3, d
                    SetBit(ref DE.hi, 3);
                    break;

                case 0xDB://set 3, e
                    SetBit(ref DE.lo, 3);
                    break;

                case 0xDC://set 3, h
                    SetBit(ref HL.hi, 3);
                    break;

                case 0xDD://set 3, l
                    SetBit(ref HL.lo, 3);
                    break;

                case 0xDE://set 3, [hl]
                    WriteMMU(HL.w, (byte)(ReadMMU(HL.w) | SET_3));
                    break;

                case 0xDF://set 3, a
                    SetBit(ref AF.hi, 3);
                    break;

                case 0xE0://set 4, b
                    SetBit(ref BC.hi, 4);
                    break;

                case 0xE1://set 4, c
                    SetBit(ref BC.lo, 4);
                    break;

                case 0xE2://set 4, d
                    SetBit(ref DE.hi, 4);
                    break;

                case 0xE3://set 4, e
                    SetBit(ref DE.lo, 4);
                    break;

                case 0xE4://set 4, h
                    SetBit(ref HL.hi, 4);
                    break;

                case 0xE5://set 4, l
                    SetBit(ref HL.lo, 4);
                    break;

                case 0xE6://set 4, [hl]
                    WriteMMU(HL.w, (byte)(ReadMMU(HL.w) | SET_4));
                    break;

                case 0xE7://set 4, a
                    SetBit(ref AF.hi, 4);
                    break;

                case 0xE8://set 5, b
                    SetBit(ref BC.hi, 5);
                    break;

                case 0xE9://set 5, c
                    SetBit(ref BC.lo, 5);
                    break;

                case 0xEA://set 5, d
                    SetBit(ref DE.hi, 5);
                    break;

                case 0xEB://set 5, e
                    SetBit(ref DE.lo, 5);
                    break;

                case 0xEC://set 5, h
                    SetBit(ref HL.hi, 5);
                    break;

                case 0xED://set 5, l
                    SetBit(ref HL.lo, 5);
                    break;

                case 0xEE://set 5, [hl]
                    WriteMMU(HL.w, (byte)(ReadMMU(HL.w) | SET_5));
                    break;

                case 0xEF://set 5, a
                    SetBit(ref AF.hi, 5);
                    break;

                case 0xF0://set 6, b
                    SetBit(ref BC.hi, 6);
                    break;

                case 0xF1://set 6, c
                    SetBit(ref BC.lo, 6);
                    break;

                case 0xF2://set 6, d
                    SetBit(ref DE.hi, 6);
                    break;

                case 0xF3://set 6, e
                    SetBit(ref DE.lo, 6);
                    break;

                case 0xF4://set 6, h
                    SetBit(ref HL.hi, 6);
                    break;

                case 0xF5://set 6, l
                    SetBit(ref HL.lo, 6);
                    break;

                case 0xF6://set 6, [hl]
                    WriteMMU(HL.w, (byte)(ReadMMU(HL.w) | SET_6));
                    break;

                case 0xF7://set 6, a
                    SetBit(ref AF.hi, 6);
                    break;

                case 0xF8://set 7, b
                    SetBit(ref BC.hi, 7);
                    break;

                case 0xF9://set 7, c
                    SetBit(ref BC.lo, 7);
                    break;

                case 0xFA://set 7, d
                    SetBit(ref DE.hi, 7);
                    break;

                case 0xFB://set 7, e
                    SetBit(ref DE.lo, 7);
                    break;

                case 0xFC://set 7, h
                    SetBit(ref HL.hi, 7);
                    break;

                case 0xFD://set 7, l
                    SetBit(ref HL.lo, 7);
                    break;

                case 0xFE://set 7, [hl]
                    WriteMMU(HL.w, (byte)(ReadMMU(HL.w) | SET_7));
                    break;

                case 0xFF://set 7, a
                    SetBit(ref AF.hi, 7);
                    break;

                    #endregion Set
            }
        }

        /// <summary>
        /// Checks for any interrupts. If an interrupt is handled, the PC is reset to the specified interrupt vector.
        /// </summary>
        private void CheckInterrupts()
        {
            InterruptType iType = interruptManager.FetchNextInterrupt(state);
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

                //Is this for reading from the int vector table (using ReadMMU for low and high bytes of address)?
                UpdateSystemTime(8);
                Push(PC.w);
                PCChange(intVector);
                interruptManager.DisableInterrupts();
                state = CPUState.Normal;
            }
        }

        /// <summary>
        /// Updates the MMU with the time, in cycles, taken since the last MMU update.
        /// </summary>
        /// <remarks>
        /// This is to ensure that certain events take place 'concurrently' with the CPU's operation.
        /// </remarks>
        /// <param name="cyclesTaken">The amount of cycles passed since the last update.</param>
        private void UpdateSystemTime(int cyclesTaken)
        {
            UpdateTimeGB(cyclesTaken);
            CyclesSinceLastStep += cyclesTaken;
        }

        #endregion CPU Actions

        #region Reading and writing

        /// <summary>
        /// Read a byte from the MMU. Takes 4 cycles to complete.
        /// </summary>
        /// <param name="address">The address of the source.</param>
        /// <returns>The byte at the address.</returns>
        private byte ReadMMU(ushort address)
        {
            byte x = ReadGB(address);
            UpdateSystemTime(4);
            return x;
        }

        /// <summary>
        /// Writes a byte to a given address. Takes 4 cycles to complete.
        /// </summary>
        /// <param name="address">The address of the destination.</param>
        /// <param name="data">The byte to write.</param>
        private void WriteMMU(ushort address, byte data)
        {
            WriteGB(address, data);
            UpdateSystemTime(4);
        }

        /// <summary>
        /// Read a byte from the specified port (in the range 0xFF00-0xFFFF). Takes 4 cycles to complete.
        /// </summary>
        /// <param name="port">The port number to read from.</param>
        /// <returns>The byte at the address.</returns>
        private byte ReadPort(byte port)
        {
            return ReadMMU((ushort)(0xFF00 | port));
        }

        /// <summary>
        /// Writes a byte to a given port (in the range 0xFF00-0xFFFF). Takes 4 cycles to complete.
        /// </summary>
        /// <param name="port">The port number to write to.</param>
        /// <param name="data">The byte to write.</param>
        private void WritePort(byte port, byte data)
        {
            WriteMMU((ushort)(0xFF00 | port), data);
        }

        /// <summary>
        /// Reads a word using the Program Counter as an address, and increments the PC. Takes 8 cycles to complete.
        /// </summary>
        /// <returns>The little-endian word at the address</returns>
        private ushort ReadPCWord()
        {
            RegisterPair ret = new RegisterPair()
            {
                lo = ReadPC()
            };
            ret.hi = ReadPC();
            return ret.w;
        }

        /// <summary>
        /// Reads a byte using the Program Counter as an address, and increments the PC. Takes 4 cycles to complete.
        /// </summary>
        /// <returns>The byte at the address.</returns>
        private byte ReadPC()
        {
            byte read = ReadMMU(PC.w);
            PC.w++;
            return read;
        }

        /// <summary>
        /// Reads a byte using the Stack Pointer as an address, and increments the SP. Takes 4 cycles to complete.
        /// </summary>
        /// <returns>The byte at the address.</returns>
        private byte ReadSP()
        {
            byte read = ReadMMU(SP.w);
            SP.w++;
            return read;
        }

        /// <summary>
        /// Writes a byte using the Stack Pointer as an address, and decrements the SP. Takes 4 cycles to complete.
        /// </summary>
        /// <param name="data">The data to write.</param>
        private void WriteSP(byte data)
        {
            SP.w--;
            WriteMMU(SP.w, data);
        }

        /// <summary>
        /// Writes a little-endian word to the given address. Takes 8 cycles to complete.
        /// </summary>
        /// <param name="dest">The address of the destination.</param>
        /// <param name="data">The word to write.</param>
        private void WriteWord(ushort dest, ushort data)
        {
            WriteMMU(dest, (byte)(data & 0xFF));
            dest++;
            WriteMMU(dest, (byte)((data >> 8) & 0xFF));
        }

        /// <summary>
        /// Changes the Program Counter to point to a new location. Takes 4 cycles to complete.
        /// </summary>
        /// <param name="newAddress">The new address the PC will point to.</param>
        private void PCChange(ushort newAddress)
        {
            PC.w = newAddress;
            UpdateSystemTime(4);
        }

        #endregion Reading and writing

        #region Instruction Implementation

        #region 8-bit Arithmetic

        /// <summary>
        /// Increments the given 8-bit register.
        /// </summary>
        /// <remarks>
        /// Z is set if result is zero.
        /// N is reset.
        /// H is set if there is carry from bit 3 to bit 4.
        /// C is unaffected.
        /// </remarks>
        /// <param name="register">The register to increment.</param>
        private void Inc8(ref byte register)
        {
            IsNegativeOp = false;
            IsHalfCarry = (((register & 0xF) + 1) & 0x10) != 0;
            register++;
            IsZero = (register == 0);
        }

        /// <summary>
        /// Decrements the given 8-bit register.
        /// </summary>
        /// <remarks>
        /// Z is set if result is zero.
        /// N is set.
        /// H is set if there is borrow from bit 4 to bit 3.
        /// C is unaffected.
        /// </remarks>
        /// <param name="register">The register to decrement.</param>
        private void Dec8(ref byte register)
        {
            IsNegativeOp = true;
            IsHalfCarry = (((register & 0xF) - 1) & 0x10) != 0;
            register--;
            IsZero = (register == 0);
        }

        /// <summary>
        /// Increments the byte at the address indicated by HL. Takes 8 cycles to complete.
        /// </summary>
        /// <remarks>
        /// Z is set if result is zero.
        /// N is reset.
        /// H is set if there is carry from bit 3 to bit 4.
        /// C is unaffected.
        /// </remarks>
        private void IncHL()
        {
            byte incHL = ReadMMU(HL.w);
            Inc8(ref incHL);
            WriteMMU(HL.w, incHL);
        }

        /// <summary>
        /// Decrements the byte at the address indicated by HL. Takes 8 cycles to complete.
        /// </summary>
        /// <remarks>
        /// Z is set if result is zero.
        /// N is reset.
        /// H is set if there is carry from bit 3 to bit 4.
        /// C is unaffected.
        /// </remarks>
        private void DecHL()
        {
            byte decHL = ReadMMU(HL.w);
            Dec8(ref decHL);
            WriteMMU(HL.w, decHL);
        }

        /// <summary>
        /// Adds the given value to A.
        /// </summary>
        /// <remarks>
        /// Z is set if the result is zero.
        /// N is reset.
        /// H is set if there is carry from bit 3 to bit 4.
        /// C is set if there is carry from bit 7.
        /// </remarks>
        /// <param name="addedValue">The value to add to A.</param>
        /// <param name="addCarry">True if the operation is an add-with-carry.</param>
        private void AddA(byte addedValue, bool addCarry)
        {
            int temp = AF.hi + addedValue + (addCarry ? (IsCarry ? 1 : 0) : 0);

            IsZero = (temp & 0xFF) == 0;
            IsNegativeOp = false;
            IsHalfCarry = (((AF.hi & 0x0F) + (addedValue & 0x0F) + (addCarry ? (IsCarry ? 1 : 0) : 0)) & 0x10) != 0;
            IsCarry = ((temp & 0x100) != 0);

            AF.hi = (byte)temp;
        }

        /// <summary>
        /// Subtracts the given value from A.
        /// </summary>
        /// <remarks>
        /// Z is set if the result is zero.
        /// N is set.
        /// H is set if there is borrow from bit 4.
        /// C is set if there is borrow from bit 8.
        /// </remarks>
        /// <param name="subtractedValue">The value to subtract from A.</param>
        /// <param name="subCarry">True if the operation is a subtract-with-carry.</param>
        private void SubA(byte subtractedValue, bool subCarry)
        {
            int temp = AF.hi - (subtractedValue + (subCarry ? IsCarry ? 1 : 0 : 0));

            IsZero = (temp & 0xFF) == 0;
            IsNegativeOp = true;
            IsHalfCarry = (((AF.hi & 0x0F) - ((subtractedValue & 0x0F) + (subCarry ? (IsCarry ? 1 : 0) : 0))) & 0x10) != 0;
            IsCarry = ((temp & 0x100) != 0);

            AF.hi = (byte)temp;
        }

        /// <summary>
        /// ANDs the given value with A.
        /// </summary>
        /// <remarks>
        /// Z is set if the result is 0.
        /// N is reset.
        /// H is set.
        /// C is reset.
        /// </remarks>
        /// <param name="value">The value to use.</param>
        private void AndA(byte value)
        {
            AF.hi &= value;
            IsZero = AF.hi == 0;
            IsNegativeOp = false;
            IsHalfCarry = true;
            IsCarry = false;
        }

        /// <summary>
        /// ORs the given value with A.
        /// </summary>
        /// <remarks>
        /// Z is set if the result is 0.
        /// N is reset.
        /// H is reset.
        /// C is reset.
        /// </remarks>
        /// <param name="value">The value to use.</param>
        private void OrA(byte value)
        {
            AF.hi |= value;
            IsZero = AF.hi == 0;
            IsNegativeOp = false;
            IsHalfCarry = false;
            IsCarry = false;
        }

        /// <summary>
        /// XORs the given value with A.
        /// </summary>
        /// <remarks>
        /// Z is set if the result is 0.
        /// N is reset.
        /// H is reset.
        /// C is reset.
        /// </remarks>
        /// <param name="value">The value to use.</param>
        private void XorA(byte value)
        {
            AF.hi ^= value;
            IsZero = AF.hi == 0;
            IsNegativeOp = false;
            IsHalfCarry = false;
            IsCarry = false;
        }

        /// <summary>
        /// Compares the given value with A. Results are equivalent to "sub a", without A changing.
        /// </summary>
        /// <remarks>
        /// Z is set if the result is zero.
        /// N is set.
        /// H is set if there is borrow from bit 4.
        /// C is set if there is borrow from bit 8.
        /// </remarks>
        /// <param name="value">The value to use.</param>
        private void CpA(byte value)
        {
            int temp = AF.hi - value;
            IsZero = (temp & 0xFF) == 0;
            IsNegativeOp = true;
            IsHalfCarry = (((AF.hi & 0x0F) - (value & 0x0F)) & 0x10) != 0;
            IsCarry = ((temp & 0x100) != 0);
        }

        /// <summary>
        /// Adjusts A to be a Binary Coded Decimal.
        /// </summary>
        /// <remarks>
        /// Z is set if A is 0.
        /// N is unaffected.
        /// H is reset.
        /// C is set/reset depending on the results.
        /// </remarks>
        private void DecimalAdjustA()
        {
            byte correction = (byte)(IsCarry ? 0x60 : 0x00);
            if (IsHalfCarry)
                correction |= 0x06;
            if (!IsNegativeOp)
            {
                if ((AF.hi & 0x0F) > 0x09)
                    correction |= 0x06;
                if (AF.hi > 0x99)
                    correction |= 0x60;
                AF.hi += correction;
            }
            else
            {
                AF.hi -= correction;
            }
            IsCarry = ((correction << 2) & 0x100) != 0;
            IsZero = AF.hi == 0;
            IsHalfCarry = false;
        }

        /// <summary>
        /// Complements A.
        /// </summary>
        /// <remarks>
        /// Z is unaffected.
        /// N is set.
        /// H is set.
        /// C is unaffected.
        /// </remarks>
        private void CPL()
        {
            IsNegativeOp = true;
            IsHalfCarry = true;
            AF.hi ^= 0xFF;
        }

        #endregion 8-bit Arithmetic

        #region 16-bit Arithmetic

        /// <summary>
        /// Increments the given 16-bit register. Takes 4 cycles to complete.
        /// </summary>
        /// <remarks>
        /// Z is unaffected.
        /// N is unaffected.
        /// H is unaffected.
        /// C is unaffected.
        /// </remarks>
        /// <param name="register">The register to increment.</param>
        private void Inc16(ref ushort register)
        {
            register++;
            UpdateSystemTime(4);
        }

        /// <summary>
        /// Decrements the given 16-bit register. Takes 4 cycles to complete.
        /// </summary>
        /// <remarks>
        /// Z is unaffected.
        /// N is unaffected.
        /// H is unaffected.
        /// C is unaffected.
        /// </remarks>
        /// <param name="register">The register to decrement.</param>
        private void Dec16(ref ushort register)
        {
            register--;
            UpdateSystemTime(4);
        }

        /// <summary>
        /// Adds the given 16-bit register to HL. Takes 4 cycle to complete.
        /// </summary>
        /// <remarks>
        /// Z is unaffected.
        /// N is reset.
        /// H is set if there is carry from bit 11.
        /// ---The half carry is done by adding the lower nibbles of
        /// ---the high bytes of HL and register, and the carry from adding
        /// ---the low bytes of HL and register.
        /// C is set if there is carry from bit 15.
        /// </remarks>
        /// <param name="register"></param>
        private void AddHL(ushort register)
        {
            int temp = HL.w + register;

            IsNegativeOp = false;
            int lowCarry = ((HL.lo + (register & 0xFF)) & 0x100) >> 8;
            IsHalfCarry = (((HL.hi & 0xF) + ((register >> 8) & 0x0F) + lowCarry) & 0x10) != 0;
            IsCarry = ((temp & 0x10000) != 0);

            HL.w += register;
            UpdateSystemTime(4);
        }

        /// <summary>
        /// Calculates the result of adding the immediate byte from the PC (as a signed value) to the Stack Pointer. Takes 8 cycles to complete.
        /// </summary>
        /// Z is reset.
        /// N is reset.
        /// H is set if there is carry from bit 3 (positive) or borrow from bit 4 (negative)
        /// C is set if there is carry from bit 7 (positive) or borrow from bit 8 (negative)
        /// <returns>SP plus the signed value.</returns>
        private ushort AddSPImmediate()
        {
            //1: Cast regular byte as sbyte - MSB is interpreted as sign bit, same value
            //2: Cast to int (signed -> signed of larger size sign-extends)
            //Ex: 1000 0000 -> 1111 1111 1111 1111 1111 1111 1000 0000
            //    0000 0000 -> 0000 0000 0000 0000 0000 0000 0000 0000
            int immediate = ((sbyte)ReadPC());//Immediate cast as 2's complement ushort.
            int sum = SP.w + immediate;
            int tempXOR = SP.w ^ immediate ^ sum;
            IsZero = false;
            IsNegativeOp = false;
            IsHalfCarry = (tempXOR & 0x10) != 0;
            IsCarry = (tempXOR & 0x100) != 0;
            UpdateSystemTime(4);
            return (ushort)sum;
        }

        /// <summary>
        /// Sets HL to SP plus the next value from the PC (as a signed value). Takes 8 cycles to complete.
        /// </summary>
        /// <remarks>
        /// Z is reset.
        /// N is reset.
        /// H is set/reset depending on the operation.
        /// C is set/reset depending on the operation.
        /// </remarks>
        private void LdHLSPN()
        {
            HL.w = AddSPImmediate();
        }

        #endregion 16-bit Arithmetic

        #region Rotate/Shift/Swap

        /// <summary>
        /// Swaps the lower and upper nibble of the given register.
        /// </summary>
        /// Z is set if result is zero.
        /// N is reset.
        /// H is reset.
        /// C is reset.
        /// <param name="register">The register to swap.</param>
        private void Swap(ref byte register)
        {
            IsZero = register == 0;
            IsNegativeOp = false;
            IsHalfCarry = false;
            IsCarry = false;
            register = (byte)((register << 4) | (register >> 4));
        }

        /// <summary>
        /// Rotates the given register left, with the carry bit as an extra position.
        /// </summary>
        /// <remarks>
        /// Z is set if result is zero.
        /// N is reset.
        /// H is reset.
        /// C contains the data that was in bit 7.
        /// </remarks>
        /// <param name="register">The register to rotate.</param>
        private void RL(ref byte register)
        {
            IsNegativeOp = false;
            IsHalfCarry = false;
            byte carry = (byte)(IsCarry ? 1 : 0);
            IsCarry = (register & 0x80) != 0;
            register = (byte)((register << 1) | carry);
            IsZero = register == 0;
        }

        /// <summary>
        /// Rotates A left, with the carry bit as an extra position.
        /// </summary>
        /// <remarks>
        /// Z is reset.
        /// N is reset.
        /// H is reset.
        /// C contains the data that was in bit 7.
        /// </remarks>
        private void RLA()
        {
            RL(ref AF.hi);
            IsZero = false;
        }

        /// <summary>
        /// Rotates the given register right, with the carry bit as an extra position.
        /// </summary>
        /// <remarks>
        /// Z is set if result is zero.
        /// N is reset.
        /// H is reset.
        /// C contains the data that was in bit 0.
        /// </remarks>
        /// <param name="register">The register to rotate.</param>
        private void RR(ref byte register)
        {
            IsNegativeOp = false;
            IsHalfCarry = false;
            byte carry = (byte)(IsCarry ? 0x80 : 0);
            IsCarry = (register & 0x1) != 0;
            register = (byte)((register >> 1) | carry);
            IsZero = register == 0;
        }

        /// <summary>
        /// Rotates A right, with the carry bit as an extra position.
        /// </summary>
        /// <remarks>
        /// Z is reset.
        /// N is reset.
        /// H is reset.
        /// C contains the data that was in bit 0.
        /// </remarks>
        private void RRA()
        {
            RR(ref AF.hi);
            IsZero = false;
        }

        /// <summary>
        /// Rotates the given register left, ignoring the carry bit.
        /// </summary>
        /// <remarks>
        /// Z is set if result is zero.
        /// N is reset.
        /// H is reset.
        /// C contains the data that was in bit 7.
        /// </remarks>
        /// <param name="register">The register to rotate.</param>
        private void RLC(ref byte register)
        {
            IsNegativeOp = false;
            IsHalfCarry = false;
            IsCarry = (register & 0x80) != 0;
            register = (byte)((register << 1) | (register >> 7));
            IsZero = (register == 0);
        }

        /// <summary>
        /// Rotates the given register left, ignoring the carry bit.
        /// </summary>
        /// <remarks>
        /// Z is reset.
        /// N is reset.
        /// H is reset.
        /// C contains the data that was in bit 7.
        /// </remarks>
        private void RLCA()
        {
            RLC(ref AF.hi);
            IsZero = false;
        }

        /// <summary>
        /// Rotates the given register right, ignoring the carry bit.
        /// </summary>
        /// <remarks>
        /// Z is set if result is zero.
        /// N is reset.
        /// H is reset.
        /// C contains the data that was in bit 0.
        /// </remarks>
        /// <param name="register">The register to rotate.</param>
        private void RRC(ref byte register)
        {
            IsNegativeOp = false;
            IsHalfCarry = false;
            IsCarry = (register & 0x1) != 0;
            register = (byte)((register >> 1) | (register << 7));
            IsZero = (register == 0);
        }

        /// <summary>
        /// Rotates A right, ignoring the carry bit.
        /// </summary>
        /// <remarks>
        /// Z is reset.
        /// N is reset.
        /// H is reset.
        /// C contains the data that was in bit 0.
        /// </remarks>
        private void RRCA()
        {
            RRC(ref AF.hi);
            IsZero = false;
        }

        /// <summary>
        /// Shifts the given register to the left. LSB becomes 0.
        /// </summary>
        /// <remarks>
        /// Z is set if result is zero.
        /// N is reset.
        /// H is reset.
        /// C contains the data that was in bit 7.
        /// </remarks>
        /// <param name="register">The register to shift.</param>
        private void SLA(ref byte register)
        {
            IsNegativeOp = false;
            IsHalfCarry = false;
            IsCarry = (register & 0x80) != 0;
            register <<= 1;
            IsZero = register == 0;
        }

        /// <summary>
        /// Shifts the given register to the right. MSB stays the same (arithmetic shift).
        /// </summary>
        /// <remarks>
        /// Z is set if result is zero.
        /// N is reset.
        /// H is reset.
        /// C contains the data that was in bit 0.
        /// </remarks>
        /// <param name="register">The register to shift.</param>
        private void SRA(ref byte register)
        {
            IsNegativeOp = false;
            IsHalfCarry = false;
            IsCarry = (register & 0x1) != 0;
            register = (byte)((register >> 1) | (register & 0x80));
            IsZero = (register == 0);
        }

        /// <summary>
        /// Shifts the given register to the right. MSB becomes 0.
        /// </summary>
        /// <remarks>
        /// Z is set if result is zero.
        /// N is reset.
        /// H is reset.
        /// C contains the data that was in bit 0.
        /// </remarks>
        /// <param name="register">The register to shift.</param>
        private void SRL(ref byte register)
        {
            IsNegativeOp = false;
            IsHalfCarry = false;
            IsCarry = (register & 0x1) != 0;
            register >>= 1;
            IsZero = (register == 0);
        }

        #endregion Rotate/Shift/Swap

        #region Bit Operations

        /// <summary>
        /// Tests the bit of the given register.
        /// </summary>
        /// <remarks>
        /// Z is set if the specific bit is 0.
        /// N is reset.
        /// H is set.
        /// C is unaffected.
        /// </remarks>
        /// <param name="register">The register to use.</param>
        /// <param name="bitNumber">The bit number to test.</param>
        private void TestBit(byte register, int bitNumber)
        {
            IsNegativeOp = false;
            IsHalfCarry = true;
            IsZero = (register & (1 << bitNumber)) == 0;
        }

        /// <summary>
        /// Sets the bit of the given register.
        /// </summary>
        /// <remarks>
        /// Z is unaffected
        /// N is unaffected.
        /// H is unaffected.
        /// C is unaffected.
        /// </remarks>
        /// <param name="register">The register to use.</param>
        /// <param name="bitNumber">The bit number to set.</param>
        private void SetBit(ref byte register, int bitNumber)
        {
            register |= (byte)(1 << bitNumber);
        }

        /// <summary>
        /// Resets the bit of the given register.
        /// </summary>
        /// <remarks>
        /// Z is unaffected
        /// N is unaffected.
        /// H is unaffected.
        /// C is unaffected.
        /// </remarks>
        /// <param name="register">The register to use.</param>
        /// <param name="bitNumber">The bit number to reset.</param>
        private void ResetBit(ref byte register, int bitNumber)
        {
            register &= (byte)(~(1 << bitNumber));
        }

        #endregion Bit Operations

        #region Jump/Call/Return

        /// <summary>
        /// Jumps to the location at the next word read from the PC. Takes 8 cycles (!isCondTrue) or 12 cycles to complete.
        /// </summary>
        /// <param name="isCondTrue">Conditional check on call (Ex: IsCarry)</param>
        private void JumpImmediate(bool isCondTrue)
        {
            ushort nextWord = ReadPCWord();
            if (isCondTrue)
            {
                PCChange(nextWord);
            }
        }

        /// <summary>
        /// Jumps to a relative offset specified from the PC. Takes 4 cycles (!isCondTrue) or 8 cycles to complete.
        /// </summary>
        /// <param name="isCondTrue">Conditional check on call (Ex: IsCarry)</param>
        private void JumpRelative(bool isCondTrue)
        {
            sbyte signedOffset = (sbyte)ReadPC();
            if (isCondTrue)
            {
                PCChange((ushort)(PC.w + signedOffset));
            }
        }

        /// <summary>
        /// Reads the next address, pushes the PC onto the stack, and jumps to the new address. Takes 8 cycles (!isCondTrue) or 20 cycles to complete.
        /// </summary>
        /// <param name="IsCondTrue">Conditional check on call (Ex: IsCarry)</param>
        private void Call(bool IsCondTrue)
        {
            ushort callAddress = ReadPCWord();
            if (IsCondTrue)
            {
                Push(PC.w);
                PCChange(callAddress);
            }
        }

        /// <summary>
        /// Pops an address off the stack, and sets the PC. This takes 12 cycles to complete.
        /// </summary>
        /// <param name="enableInterrupts">Set if this is a RETI instruction.</param>
        private void Return(bool enableInterrupts)
        {
            ushort returnAddress = 0;
            Pop(ref returnAddress);
            PCChange(returnAddress);
            if (enableInterrupts)
            {
                interruptManager.EnableInterrupts();
            }
        }

        /// <summary>
        /// "Returns" if the condition is true. This takes 4 cycles (!isCondTrue) or 16 cycles to complete.
        /// </summary>
        /// <param name="isCondTrue">Conditional check on call (Ex: IsCarry)</param>
        private void CheckedReturn(bool isCondTrue)
        {
            UpdateSystemTime(4);
            if (isCondTrue)
            {
                Return(false);
            }
        }

        /// <summary>
        /// Pushes the current address and jumps to the given jump vector. Takes 12 cycles to complete.
        /// </summary>
        /// <param name="jumpVector">The vector to jump to.</param>
        private void Reset(byte jumpVector)
        {
            Push(PC.w);
            PCChange((ushort)(jumpVector & 0x38));
        }

        #endregion Jump/Call/Return

        #region CPU Commands

        /// <summary>
        /// Halts the CPU.
        /// </summary>
        private void Halt()
        {
            if (!interruptManager.InterruptMasterEnable && interruptManager.InterruptsReady)
            {
                //if (mmu.IsCGB) CycleCounter += 4;
                /*else*/
                RepeatLastInstruction = true;
            }
            else
            {
                state = CPUState.Halt;
            }
        }

        /// <summary>
        /// Stops the CPU.
        /// </summary>
        private void Stop()
        {
            //Stop doesn't increment the PC, and turns off the LCD(not implemented.)
            //Also, speed switch occurs after stop is used (CGB, not implemented.).
            //state = CPUState.Stop;
            PC.w++;
        }

        /// <summary>
        /// Disables interrupts.
        /// </summary>
        private void DisableInterrupts()
        {
            interruptManager.DisableInterrupts();
        }

        /// <summary>
        /// Enables interrupts.
        /// </summary>
        private void EnableInterrupts()
        {
            interruptManager.EnableInterrupts();
        }

        /// <summary>
        /// Complements the carry flag.
        /// </summary>
        /// <remarks>
        /// Z is unaffected.
        /// N is reset.
        /// H is reset.
        /// C is flipped.
        /// </remarks>
        private void ComplementCarryFlag()
        {
            IsHalfCarry = false;
            IsNegativeOp = false;
            IsCarry = !IsCarry;
        }

        /// <summary>
        /// Sets the carry flag.
        /// </summary>
        /// <remarks>
        /// Z is unaffected.
        /// N is reset.
        /// H is reset.
        /// C is set.
        /// </remarks>
        private void SetCarryFlag()
        {
            IsNegativeOp = false;
            IsHalfCarry = false;
            IsCarry = true;
        }

        #endregion CPU Commands

        #region Stack Commands

        /// <summary>
        /// Pushes a given word to the stack. Takes 8 cycles to complete.
        /// </summary>
        /// <remarks>
        /// Pushes the high byte first, then the low byte.
        /// </remarks>
        /// <param name="pushData">The word to push.</param>
        private void Push(ushort pushData)
        {
            WriteSP((byte)(pushData >> 8));
            WriteSP((byte)(pushData));
        }

        /// <summary>
        /// Pushes a given register to the stack. Takes 12 cycles to complete.
        /// </summary>
        /// <remarks>
        /// Pushes the high byte first, then the low byte.
        /// </remarks>
        /// <param name="pushData">The word to push.</param>
        private void PushRegister(ushort pushData)
        {
            Push(pushData);
            UpdateSystemTime(4);
        }

        /// <summary>
        /// Pops a word off the stack to the given location. Takes 8 cycles to complete.
        /// </summary>
        /// <remarks>
        /// Pops the low byte first, then the high byte.
        /// </remarks>
        /// <param name="popLoc">The location to pop to.</param>
        private void Pop(ref ushort popLoc)
        {
            popLoc = ReadSP();
            popLoc |= (ushort)(ReadSP() << 8);
        }

        #endregion Stack Commands

        #endregion Instruction Implementation
    }
}