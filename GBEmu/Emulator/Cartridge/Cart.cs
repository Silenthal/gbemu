using GBEmu.Emulator.Debug;
using GBEmu.Emulator.IO;
using GBEmu.Emulator.Timing;
using System;

namespace GBEmu.Emulator.Cartridge
{
    public abstract class Cart : IReadWriteCapable
    {
        protected byte[] romFile;
        protected int MaxRamBank;

        protected bool RamEnabled = false;
        protected int RomBank;
        protected int MaxRomBank;

        protected CartFeatures features;

        protected bool BatteryPresent
        {
            get
            {
                return (features & CartFeatures.BatteryBacked) == CartFeatures.BatteryBacked;
            }
        }

        protected bool RumblePresent
        {
            get
            {
                return (features & CartFeatures.Rumble) == CartFeatures.Rumble;
            }
        }

        protected bool TimerPresent
        {
            get
            {
                return (features & CartFeatures.Timer) == CartFeatures.Timer;
            }
        }

        protected byte[] CartRam;
        protected int CartRamBank;

        protected Cart(byte[] inFile, CartFeatures cartFeatures)
        {
            features = cartFeatures;
            romFile = new byte[inFile.Length];
            Array.Copy(inFile, romFile, inFile.Length);
            MaxRomBank = romFile.Length >> 14;
            InitializeOutsideRAM();
            RomBank = 1;
        }

        protected virtual void InitializeOutsideRAM()
        {
            CartRamBank = 0;
            MaxRamBank = 0;
            switch (romFile[0x149])
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

        public byte Read(int position)
        {
            position &= 0xFFFF;
            if (position < 0x4000)
            {
                return romFile[position];
            }
            else if (position < 0x8000)
            {
                return romFile[(RomBank << 14) | (position & 0x3FFF)];
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
                return CartRam[(CartRamBank << 13) | position];
            }
            else
            {
                Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, GlobalTimer.GetInstance().GetTime(), position.ToString("X4"), "Disabled RAM Read Attempt"));
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
            if (RamEnabled)
            {
                if ((position - 0xA000) >= CartRam.Length)
                {
                    Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, GlobalTimer.GetInstance().GetTime(), position.ToString("X4"), "RAM Write Failed"));
                    return;
                }
                CartRam[(CartRamBank * 0x2000) + (position - 0xA000)] = value;
            }
            else
            {
                Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, GlobalTimer.GetInstance().GetTime(), position.ToString("X4"), "Disabled RAM Write Attempt[" + value.ToString("X2") + "]."));
            }
        }
    }
}