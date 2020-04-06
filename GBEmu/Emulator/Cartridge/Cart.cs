using GBEmu.Emulator.IO;
using System;

namespace GBEmu.Emulator.Cartridge
{
    public abstract class Cart : IReadWriteCapable
    {
        protected byte[] romFile;
        protected int MaxRomBank;

        protected int RomBank = 1;

        protected bool RamEnabled = false;

        protected byte[] CartRam;
        protected int MaxRamBank;

        protected int CartRamBank = 0;

        private CartFeatures features;

        protected bool BatteryPresent => (features & CartFeatures.BatteryBacked) != 0;

        protected bool RumblePresent => (features & CartFeatures.Rumble) != 0;

        protected bool TimerPresent => (features & CartFeatures.Timer) != 0;

        protected Cart(byte[] inFile, CartFeatures cartFeatures)
        {
            features = cartFeatures;
            romFile = new byte[inFile.Length];
            Array.Copy(inFile, romFile, inFile.Length);

            int actualMax = romFile.Length >> 14;
            byte reportedSizeCode = romFile[HeaderConstants.RomSizeOffset];
            int reportedMax;
            if (reportedSizeCode == 0)
            {
                reportedMax = 0;
            }
            else if (reportedSizeCode >= 1 && reportedSizeCode <= 8)
            {
                reportedMax = 2 << reportedSizeCode;
            }
            else if (reportedSizeCode >= 0x52 && reportedSizeCode <= 0x54)
            {
                reportedMax = 64 + (8 << (reportedSizeCode - 0x52));
            }
            else
            {
                reportedMax = actualMax;
            }
            MaxRomBank = Math.Min(actualMax, reportedMax);
            RomBank = 1;

            InitializeOutsideRAM();
        }

        protected virtual void InitializeOutsideRAM()
        {
            MaxRamBank = 0;
            switch (romFile[HeaderConstants.RamSizeOffset])
            {
                case 0x00://No RAM?
                    MaxRamBank = 2;
                    break;

                case 0x01://A000-A7FF, 2 kilobytes
                case 0x02://A000-BFFF, 8 kilobytes
                    MaxRamBank = 1;
                    break;

                case 0x03://A000-BFFF x 4, 32 kilobytes
                    MaxRamBank = 4;
                    break;

                default:
                    MaxRamBank = 16;
                    break;
            }
            CartRam = new byte[MaxRamBank * 0x2000];
            CartRamBank = 0;
        }

        public byte[] SaveOutsideRAM()
        {
            byte[] ret = new byte[CartRam.Length];
            Array.Copy(CartRam, ret, CartRam.Length);
            return ret;
        }

        public void LoadOutsideRAM(byte[] ram)
        {
            if (CartRam != null)
            {
                int copyLength = CartRam.Length <= ram.Length ? CartRam.Length : ram.Length;
                for (int i = 0; i < copyLength; i++)
                {
                    CartRam[i] = ram[i];
                }
            }
        }

        public virtual byte Read(int position)
        {
            position &= 0xFFFF;
            if (position < 0x4000)
            {
                return romFile[position];
            }
            else if (position < 0x8000)
            {
                return romFile[((RomBank << 14) | (position & 0x3FFF)) % romFile.Length];
            }
            else if (position >= 0xA000 && position < 0xC000)
            {
                return CartRamRead(position);
            }
            else
            {
                return 0xFF;
            }
        }

        protected virtual byte CartRamRead(int position)
        {
            position -= 0xA000;
            if (RamEnabled)
            {
                return CartRam[((CartRamBank << 13) | position) % CartRam.Length];
            }
            else
            {
                return 0xFF;
            }
        }

        public void Write(int position, byte value)
        {
            position &= 0xFFFF;
            if (position < 0x8000)
            {
                MBCWrite(position, value);
            }
            else if (position >= 0xA000 && position < 0xC000)
            {
                CartRamWrite(position, value);
            }
        }

        protected abstract void MBCWrite(int position, byte value);

        protected virtual void CartRamWrite(int position, byte value)
        {
            position -= 0xA000;
            if (RamEnabled)
            {
                CartRam[(CartRamBank << 13 | position) % CartRam.Length] = value;
            }
        }
    }
}