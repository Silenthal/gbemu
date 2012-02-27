using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GBRead.Emulator
{
    class Cartridge
    {
        enum CartridgeType : byte
        {
            ROM = 0x0,
            MBC1 = 0x1,
            MBC1_R = 0x2,
            MBC1_RB = 0x3,
            MBC2 = 0x5,
            MBC2_B = 0x6,
            ROM_R = 0x8,
            ROM_RB = 0x9,
            MMM01 = 0x0B,
            MMM01_R = 0x0C,
            MMM01_RB = 0x0D,
            MBC3_TB = 0x0F,
            MBC3_TRB = 0x10,
            MBC3 = 0x11,
            MBC3_R = 0x12,
            MBC3_RB = 0x13,
            MBC4 = 0x15,
            MBC4_R = 0x16,
            MBC4_RB = 0x17,
            MBC5 = 0x19,
            MBC5_R = 0x1A,
            MBC5_RB = 0x1B,
            MBC5_M = 0x1C,
            MBC5_MR = 0x1D,
            MBC5_MRB = 0x1E,
            PC = 0xFC,
            TAMA5 = 0xFD,
            HuC3 = 0xFE,
            HuC1_RB = 0xFF
        }
        
        private byte[] romFile;
        public byte[] ROMFile { get { return romFile; } }
        public bool FileLoaded { get; private set; }
        private bool RamEnabled = false;
        private int bankNum = 1;

        CartridgeType internalMBC;
        
        public bool RamPresent { get; private set; }
        public bool BatteryPresent { get; private set; }
        public bool RumblePresent { get; private set; }
        public bool TimerPresent { get; private set; }

        private byte[,] externalRamMap = new byte[1, 0x2000]; // 0xA000 - 0xBFFF
        private byte externalRamBank = 0;

        private bool mbc1_IsMemoryModel_16_8;
        private bool mbc5_IsRumble;

        private bool IsLatchOn = false;
        private byte[] rtc = new byte[5];
        private DateTime LastLatchTime;
        private byte[] lastRtc = new byte[5];
        private byte rtcReg = 0;
        private byte lastWrittenVal = 0xFF;


        public Cartridge(byte[] inFile)
        {
            romFile = new byte[inFile.Length];
            Array.Copy(inFile, romFile, inFile.Length);
            SetMBC();
            InitializeOutsideRAM();
            FileLoaded = true;
            mbc1_IsMemoryModel_16_8 = true;
        }

        public void ClockTick()
        {
            //RTC registers are like so:
            //[0] = Seconds
            //[1] = Minutes
            //[2] = Hours
            //[3] = Days (lower 8)
            //[4] = Days (upper 1)
            //If value written to activate is 1, and 0 was written before, latch rtc.
            //If clock was halted, then latch the last time. Else, latch a 
            //new time where the rtc regs will equal the time last accessed, 
            //plus the time difference between then and now.
            
            //If Stop Timer (6th bit of DH) is set, return with the old time in rtc.
            if ((rtc[4] & 0x40) == 1)
            {
                return;
            }
            //Else, latch new time.
            TimeSpan s = new TimeSpan(((rtc[4] & 1) << 9) | rtc[3], rtc[2], rtc[1], rtc[0]);
            DateTime temp = DateTime.Now;
            s += temp - LastLatchTime;
            LastLatchTime = temp;
            bool over = false;
            if (s > TimeSpan.FromDays(511))
            {
                //Difference is greater than 511, so oveflow is present.
                while (s.Days > 511)
                {
                    s -= TimeSpan.FromDays(511);
                }
                over = true;
            }
            //Write new times.
            rtc[0] = (byte)s.Seconds;
            rtc[1] = (byte)s.Minutes;
            rtc[2] = (byte)s.Hours;
            rtc[3] = (byte)s.Days;
            rtc[4] = (byte)((rtc[4] & 0xFE) | (s.Days >> 8));
            if (over)
            {
                //Overflow will set the last bit of DH
                rtc[4] |= 0x80;
            }
        }

        private void SetMBC()
        {
            CartridgeType cs = (CartridgeType)romFile[0x147];
            BatteryPresent = false;
            RamPresent = false;
            RumblePresent = false;
            TimerPresent = false;
            #region MBC
            switch (cs)
            {
                case CartridgeType.ROM:
                    internalMBC = CartridgeType.ROM;
                    break;
                case CartridgeType.MBC1:
                    internalMBC = CartridgeType.MBC1;
                    break;
                case CartridgeType.MBC1_R:
                    internalMBC = CartridgeType.MBC1;
                    RamPresent = true;
                    break;
                case CartridgeType.MBC1_RB:
                    internalMBC = CartridgeType.MBC1;
                    RamPresent = BatteryPresent = true;
                    break;
                case CartridgeType.MBC2:
                    internalMBC = CartridgeType.MBC2;
                    break;
                case CartridgeType.MBC2_B:
                    internalMBC = CartridgeType.MBC2;
                    BatteryPresent = true;
                    break;
                case CartridgeType.ROM_R:
                    internalMBC = CartridgeType.ROM;
                    RamPresent = true;
                    break;
                case CartridgeType.ROM_RB:
                    internalMBC = CartridgeType.ROM;
                    RamPresent = BatteryPresent = true;
                    break;
                case CartridgeType.MMM01:
                    internalMBC = CartridgeType.MMM01;
                    break;
                case CartridgeType.MMM01_R:
                    internalMBC = CartridgeType.MMM01;
                    RamPresent = true;
                    break;
                case CartridgeType.MMM01_RB:
                    internalMBC = CartridgeType.MMM01;
                    RamPresent = BatteryPresent = true;
                    break;
                case CartridgeType.MBC3_TB:
                    internalMBC = CartridgeType.MBC3;
                    TimerPresent = BatteryPresent = true;
                    break;
                case CartridgeType.MBC3_TRB:
                    internalMBC = CartridgeType.MBC3;
                    TimerPresent = RamPresent = BatteryPresent = true;
                    break;
                case CartridgeType.MBC3:
                    internalMBC = CartridgeType.MBC3;
                    break;
                case CartridgeType.MBC3_R:
                    internalMBC = CartridgeType.MBC3;
                    RamPresent = true;
                    break;
                case CartridgeType.MBC3_RB:
                    internalMBC = CartridgeType.MBC3;
                    RamPresent = BatteryPresent = true;
                    break;
                case CartridgeType.MBC4:
                    internalMBC = CartridgeType.MBC4;
                    break;
                case CartridgeType.MBC4_R:
                    internalMBC = CartridgeType.MBC4;
                    RamPresent = true;
                    break;
                case CartridgeType.MBC4_RB:
                    internalMBC = CartridgeType.MBC4;
                    RamPresent = BatteryPresent = true;
                    break;
                case CartridgeType.MBC5:
                    internalMBC = CartridgeType.MBC5;
                    break;
                case CartridgeType.MBC5_R:
                    internalMBC = CartridgeType.MBC5;
                    RamPresent = true;
                    break;
                case CartridgeType.MBC5_RB:
                    internalMBC = CartridgeType.MBC5;
                    RamPresent = BatteryPresent = true;
                    break;
                case CartridgeType.MBC5_M:
                    internalMBC = CartridgeType.MBC5;
                    RumblePresent = true;
                    break;
                case CartridgeType.MBC5_MR:
                    internalMBC = CartridgeType.MBC5;
                    RumblePresent = RamPresent = true;
                    break;
                case CartridgeType.MBC5_MRB:
                    internalMBC = CartridgeType.MBC5;
                    RumblePresent = RamPresent = BatteryPresent = true;
                    break;
                case CartridgeType.PC:
                    internalMBC = CartridgeType.PC;
                    break;
                case CartridgeType.TAMA5:
                    internalMBC = CartridgeType.TAMA5;
                    break;
                case CartridgeType.HuC3:
                    internalMBC = CartridgeType.HuC3;
                    break;
                case CartridgeType.HuC1_RB:
                    internalMBC = CartridgeType.HuC1_RB;
                    RamPresent = BatteryPresent = true;
                    break;
                default:
                    internalMBC = CartridgeType.ROM;
                    break;
            }
            #endregion
            
        }

        private void InitializeOutsideRAM()
        {
            if (RamPresent)
            {
                switch (internalMBC)
                {
                    case CartridgeType.MBC2:
                        externalRamMap = new byte[1, 0x200];//A000-A1FF, lower nibble only
                        break;
                    default:
                        switch (romFile[0x149])
                        {
                            case 0x01:
                                externalRamMap = new byte[1, 0x800];//A000-A7FF, 2 kilobytes
                                break;
                            case 0x02:
                                externalRamMap = new byte[1, 0x2000];//A000-BFFF, 8 kilobytes
                                break;
                            case 0x03:
                                externalRamMap = new byte[4, 0x2000];//A000-BFFF x 4, 32 kilobytes
                                break;
                            default:
                                externalRamMap = new byte[0, 0];
                                break;
                        }
                        break;
                }
            }
            else
            {
                externalRamMap = new byte[0, 0];
            }
        }

        //private void InitializeRomSize()
        //{
        //    switch (romFile[0x148])
        //    {
        //        case 0x00:
        //            MaxRomBank = 2;
        //            break;
        //        case 0x01:
        //            MaxRomBank = 4;
        //            break;
        //        case 0x02:
        //            MaxRomBank = 8;
        //            break;
        //        case 0x03:
        //            MaxRomBank = 16;
        //            break;
        //        case 0x04:
        //            MaxRomBank = 32;
        //            break;
        //        case 0x05:
        //            MaxRomBank = 64;
        //            break;
        //        case 0x06:
        //            MaxRomBank = 128;
        //            break;
        //        case 0x07:
        //            MaxRomBank = 256;
        //            break;
        //        case 0x52:
        //            MaxRomBank = 72;
        //            break;
        //        case 0x53:
        //            MaxRomBank = 80;
        //            break;
        //        case 0x54:
        //            MaxRomBank = 96;
        //            break;
        //    }
        //}

        public byte Read(int position)
        {
            if (position < 0x4000) return romFile[position];
            else if (position < 0x8000)
            {
                switch (internalMBC)
                {
                    case CartridgeType.MBC1:
                        if (mbc1_IsMemoryModel_16_8)
                        {
                            int bknm = (bankNum & 0x1F) | (externalRamBank & 0x03);
                            return romFile[(bknm * 0x4000) + position - 0x4000];
                        }
                        else return romFile[(bankNum * 0x4000) + position - 0x4000];
                    default:
                        return romFile[(bankNum * 0x4000) + position - 0x4000];
                }
            }
            else if (position > 0x9FFF && position < 0xC000 && RamEnabled)
            {
                switch (internalMBC)
                {
                    case CartridgeType.MBC1:
                        if (mbc1_IsMemoryModel_16_8) return externalRamMap[0, position - 0xA000];
                        else return externalRamMap[externalRamBank, position - 0xA000];
                    default:
                        return externalRamMap[externalRamBank, position - 0xA000];
                }
            }
            else return 0;
        }

        public void Write(int position, byte value)
        {
            #region 0000-1FFF
            if (position < 0x2000)
            {
                switch (internalMBC)
                {
                    case CartridgeType.MBC1:
                    case CartridgeType.MBC3:
                    case CartridgeType.MBC5:
                        RamEnabled = ((value & 0x0F) == 0x0A);
                        break;
                    case CartridgeType.MBC2:
                        if ((position & 0x100) == 0)
                        {
                            RamEnabled = ((value & 0x0F) == 0x0A);
                        }
                        break;
                    default:
                        break;
                }
            }
            #endregion
            #region 2000-2FFF
            else if (position < 0x3000 && internalMBC == CartridgeType.MBC5)
            {
                bankNum = (bankNum & 0x100) | value;
            }
            #endregion
            #region 3000-3FFF
            else if (position < 0x4000)
            {
                //Writes to 2000-3FFF will:
                //MBC1: Write ROM Bank number ***BBBBB (0 => 1)
                //MBC2: Write ROM Bank number, if addr & 0x100 is 1 ****BBBB
                //MBC3: Write ROM Bank number *BBBBBBB
                //MBC5: Write ROM Low Bank number (if write to 2000-2FFF) BBBBBBBB
                //MBC5: Write ROM High Bank number (if write to 3000-3FFF) *******B
                switch(internalMBC)
                {
                    case CartridgeType.MBC1:
                        if (value == 0)
                        {
                            bankNum = 1;
                        }
                        else bankNum = (value & 0x1F);
                        break;
                    case CartridgeType.MBC2:
                        if ((position & 0x100) != 0)
                        {
                            bankNum = value & 0x0F;
                        }
                        break;
                    case CartridgeType.MBC3:
                        bankNum = value & 0x7F;
                        break;
                    case CartridgeType.MBC5:
                        bankNum = bankNum & 0xFF | ((value & 1) << 8);
                        break;
                    default:
                        bankNum = value & 0x0F;
                        break;
                }
            }
            #endregion
            #region 4000-5FFF
            else if (position < 0x6000 && internalMBC != CartridgeType.MBC2)
            {
                //Writes to 4000-5FFF will:
                //MBC1: Write RAM Bank number ******BB (4/32)
                //MBC1: Write ROM upper bank num ******BB (16/8)
                //MBC5: Write RAM Bank number ****BBBB (no rumble present)
                //MBC5: Write RAM Bank number + enable/disable rumble ****MBBB (M = 0, 1 for off/on) (B for bank)
                //MBC3: Write RTC register number if val is >= 0x8
                //MBC3: Write RAM Bank number ******BB
                switch(internalMBC)
                {
                    case CartridgeType.MBC1:
                        externalRamBank = (byte)(value & 0x3);
                        break;
                    case CartridgeType.MBC3:
                        if (TimerPresent && ((value & 8) != 0) && value <= 0xD)
                        {
                            rtcReg = (byte)(value & 0x7);
                        }
                        else externalRamBank = (byte)(value & 0x03);
                        break;
                    case CartridgeType.MBC5:
                        if (RumblePresent)
                        {
                            externalRamBank = (byte)(value & 0x07);
                            mbc5_IsRumble = (value & 0x08) == 1;
                        }
                        else externalRamBank = (byte)(value & 0x0F);
                        break;
                    default:
                        break;
                }
            }
            #endregion
            #region 6000-7FFF
            else if (position < 0x8000 && RamEnabled)
            {
                //Writes to 6000-7FFF will:
                //MBC1: Change memory model from 16/8 to 4/32
                //MBC5: Control latch of RTC
                switch (internalMBC)
                {
                    case CartridgeType.MBC1:
                        mbc1_IsMemoryModel_16_8 = ((value & 1) == 0);
                        break;
                    case CartridgeType.MBC2:
                        if (TimerPresent)
                        {
                            if (value == 0)
                            {
                                IsLatchOn = false;
                                lastWrittenVal = value;
                            }
                            else if (value == 1 && lastWrittenVal == 0)
                            {
                                IsLatchOn = true;
                                lastWrittenVal = value;
                                ClockTick();
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            #endregion
            #region A000-BFFF
            else if (position > 0x9FFF & position < 0xC000 & RamEnabled)
            {
                //Possibly add another condition for HuC1

                //Writes to A000-BFFF will:
                //MBC1: 
                switch (internalMBC)
                {
                    case CartridgeType.MBC2:
                        if (position - 0xA000 < 0x800)
                        {
                            externalRamMap[0, position - 0xA000] = (byte)(value | 0xF0);
                        }
                        break;
                    case CartridgeType.MBC3:
                        if (TimerPresent && IsLatchOn)
                        {
                            rtc[rtcReg] = value;
                        }
                        else externalRamMap[externalRamBank, position - 0xA000] = value;
                        break;
                    default:
                        externalRamMap[externalRamBank, position - 0xA000] = value;
                        break;
                }
            }
            #endregion
        }

        public string GetHeaderInfo()
        {
            #region Get Title
            StringBuilder title = new StringBuilder("");
            for (int i = 0; i < (ROMFile[0x143] == 0 ? 15 : 11); i++)
            {
                if (ROMFile[i + 0x134] == 0) title.Append(" ");
                else title.Append((char)ROMFile[i + 0x134]);
            }
            #endregion

            #region Get Manufacturer Code
            StringBuilder mCode = new StringBuilder("");
            if (ROMFile[0x143] != 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    char fCode = ROMFile[0x13F + i] == 0 ? '_' : (char)ROMFile[0x13F + i];
                    mCode.Append(fCode);
                }
            }
            else mCode.Append("--");
            #endregion

            #region Get Cartridge Type
            string cartridgeType = "";
            switch (ROMFile[0x143])
            {
                case 0x0:
                    cartridgeType = "Game Boy";
                    break;
                case 0x80:
                    cartridgeType = "Game Boy Color, Game Boy Compatible";
                    break;
                case 0x84:
                    cartridgeType = "Colorized Game Boy";
                    break;
                case 0x88:
                    cartridgeType = "Colorized Game Boy";
                    break;
                case 0xC0:
                    cartridgeType = "Game Boy Color Only";
                    break;
                default:
                    cartridgeType = "Unrecognized";
                    break;
            }
            #endregion

            #region Get License Code
            string licenseCode = "";
            if (ROMFile[0x14B] == 0x33)
                licenseCode = ROMFile[0x144].ToString("X2") + ROMFile[0x145].ToString("X2");
            else licenseCode = ROMFile[0x143].ToString("X2");
            #endregion

            #region Get Rom Type
            string romType = "";
            switch (ROMFile[0x147])
            {
                case 0x00:
                    romType = "ROM Only";
                    break;
                case 0x1:
                    romType = "MBC1";
                    break;
                case 0x2:
                    romType = "MBC1 + RAM";
                    break;
                case 0x3:
                    romType = "MBC1 + RAM + Battery";
                    break;
                case 0x5:
                    romType = "MBC2";
                    break;
                case 0x6:
                    romType = "MBC2 + Battery";
                    break;
                case 0x8:
                    romType = "ROM + RAM";
                    break;
                case 0x9:
                    romType = "ROM + RAM + Battery";
                    break;
                case 0xB:
                    romType = "MMM01";
                    break;
                case 0xC:
                    romType = "MMM01 + RAM";
                    break;
                case 0xD:
                    romType = "MMM01 + RAM + Battery";
                    break;
                case 0xF:
                    romType = "MBC3 + Timer + Battery";
                    break;
                case 0x10:
                    romType = "MBC3 + Timer + RAM + Battery";
                    break;
                case 0x11:
                    romType = "MBC3";
                    break;
                case 0x12:
                    romType = "MBC3 + RAM";
                    break;
                case 0x13:
                    romType = "MBC3 + RAM + Battery";
                    break;
                case 0x15:
                    romType = "MBC4";
                    break;
                case 0x16:
                    romType = "MBC4 + RAM";
                    break;
                case 0x17:
                    romType = "MBC4 + RAM + Battery";
                    break;
                case 0x19:
                    romType = "MBC5";
                    break;
                case 0x1A:
                    romType = "MBC5 + RAM";
                    break;
                case 0x1B:
                    romType = "MBC5 + RAM + Battery";
                    break;
                case 0x1C:
                    romType = "MBC5 + Rumble";
                    break;
                case 0x1D:
                    romType = "MBC5 + Rumble + RAM";
                    break;
                case 0x1E:
                    romType = "MBC5 + Rumble + RAM + Battery";
                    break;
                case 0xFC:
                    romType = "Pocket Camera";
                    break;
                case 0xFD:
                    romType = "Bandai TAMA5";
                    break;
                case 0xFE:
                    romType = "HuC3";
                    break;
                case 0xFF:
                    romType = "HuC1 + RAM + Battery";
                    break;
                default:
                    romType = "Unrecognized";
                    break;
            }
            #endregion

            #region ROM Size
            string ROMSize = String.Empty;
            switch (ROMFile[0x148])
            {
                case 0:
                    ROMSize = "256 kBit/32 KB";//2 banks
                    break;
                case 1:
                    ROMSize = "512 kBit/64 KB";//4 banks
                    break;
                case 2:
                    ROMSize = "1 mBit/128 KB";//8 banks
                    break;
                case 3:
                    ROMSize = "2 mBit/256 KB";//16 banks
                    break;
                case 4:
                    ROMSize = "4 mBit/512 KB";//32 banks
                    break;
                case 5:
                    ROMSize = "8 mBit/1 MB";//64 banks
                    break;
                case 6:
                    ROMSize = "16 mBit/2 MB";//128 banks
                    break;
                case 7:
                    ROMSize = "32 mBit/4 MB";//256 banks
                    break;
                case 0x52:
                    ROMSize = "9 mBit/1.125 MB";//72 banks
                    break;
                case 0x53:
                    ROMSize = "10 mBit/1.25 MB";//80 banks
                    break;
                case 0x54:
                    ROMSize = "12 mBit/1.5 MB";//96 banks
                    break;
                default:
                    ROMSize = "Unrecognized size";
                    break;
            }
            #endregion

            #region RAM Size
            string RAMSize = "";
            switch (ROMFile[0x149])
            {
                case 0:
                    RAMSize = "None";
                    break;
                case 1:
                    RAMSize = "2 KB";
                    break;
                case 2:
                    RAMSize = "8 KB";
                    break;
                case 3:
                    RAMSize = "16 KB";
                    break;
                default:
                    RAMSize = "Unrecognized value";
                    break;
            }
            #endregion

            StringBuilder info = new StringBuilder();
            info.AppendLine("Rom Title: " + title.ToString());
            if (mCode.ToString() != "--") info.AppendLine("Manufacturer Code (CGB): " + mCode.ToString());
            info.AppendLine("Cartridge Type: " + cartridgeType);
            info.AppendLine("Licensee Code: " + licenseCode);
            info.AppendLine("Super GB Functions: " + (ROMFile[0x146] == 0x03 ? "Yes" : "No"));
            info.AppendLine("Reported ROM/Memory Bank Controller Type: " + romType);
            info.AppendLine("Reported ROM Size: " + ROMSize);
            info.AppendLine("Reported RAM Size: " + RAMSize);
            info.AppendLine("Country Code: " + (ROMFile[0x14a] == 0 ? "Japanese" : "Non-Japanese"));
            info.AppendLine(String.Format("Version Number: {0:X2}", ROMFile[0x14C]));
            return info.ToString();
        }
    }
}
