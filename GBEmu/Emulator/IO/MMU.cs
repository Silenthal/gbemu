using GBEmu.Emulator.Audio;
using GBEmu.Emulator.Cartridge;
using GBEmu.Emulator.Graphics;
using GBEmu.Emulator.Input;
using GBEmu.Emulator.Memory;
using GBEmu.Emulator.Timing;

namespace GBEmu.Emulator.IO
{
    public class MMU : TimedIODevice
    {
        #region System Components

        private InterruptManager interruptManager;
        private Video LCD;
        private Serial serial;
        private GBTimer timer;
        private Cart cart;
        private GBInput input;
        private GBAudio audio;
        private WRAM wram;
        private HRAM hram;

        #endregion System Components

        public MMU(InterruptManager iM,
            Cart iCart,
            GBInput iInput,
            GBAudio iAudio,
            GBTimer iTimer,
            Serial iSerial,
            Video iVideo,
            WRAM iWram,
            HRAM iHram)
        {
            interruptManager = iM;
            cart = iCart;
            input = iInput;
            timer = iTimer;
            LCD = iVideo;
            serial = iSerial;
            audio = iAudio;
            wram = iWram;
            hram = iHram;
        }

        #region Reads

        public override byte Read(int position)
        {
            position &= 0xFFFF;
            if (position < 0x8000)
                return cart.Read(position);
            else if (position < 0xA000)
                return LCD.Read(position);
            else if (position < 0xC000)
                return cart.Read(position);
            else if (position < 0xE000)
                return wram.Read(position);
            else if (position < 0xFE00)
                return wram.Read(position - 0x2000);
            else if (position < 0xFEA0)
                return LCD.Read(position);
            else if (position < 0xFF00)
                return 0xFF;
            else
            {
                if (position < 0xFF80)
                {
                    switch (position & 0xFF)
                    {
                        #region Input Read

                        case IOPorts.P1:
                            return input.Read(position);

                        #endregion Input Read

                        #region Serial Read

                        case IOPorts.SB:
                        case IOPorts.SC:
                            return serial.Read(position);

                        #endregion Serial Read

                        #region Timer Read

                        case IOPorts.DIV:
                        case IOPorts.TIMA:
                        case IOPorts.TMA:
                        case IOPorts.TAC:
                            return timer.Read(position);

                        #endregion Timer Read

                        #region Interrupt Manager Read

                        case IOPorts.IF:
                            return interruptManager.Read(position);

                        #endregion Interrupt Manager Read

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

                        #endregion Audio Read

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

                        #endregion Video Read

                        default:
                            return 0xFF;
                    }
                }
                else if (position < 0xFFFF)
                {
                    return hram.Read(position);
                }
                else
                {
                    return interruptManager.Read(position);
                }
            }
        }

        #endregion Reads

        #region Writes

        public override void Write(int position, byte value)
        {
            position &= 0xFFFF;
            if (position < 0x8000)
                cart.Write(position, value);
            else if (position < 0xA000)
                LCD.Write(position, value);
            else if (position < 0xC000)
                cart.Write(position, value);
            else if (position < 0xE000)
                wram.Write(position, value);
            else if (position < 0xFE00)
                return;
            else if (position < 0xFEA0)
                LCD.Write(position, value);
            else if (position < 0xFF00)
                return;
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

                        #endregion Writes to Input

                        #region Writes to Serial

                        case IOPorts.SB:
                        case IOPorts.SC:
                            serial.Write(position, value);
                            break;

                        #endregion Writes to Serial

                        #region Writes to Timer

                        case IOPorts.DIV:
                        case IOPorts.TIMA:
                        case IOPorts.TMA:
                        case IOPorts.TAC:
                            timer.Write(position, value);
                            break;

                        #endregion Writes to Timer

                        #region Writes to InterruptManager

                        case IOPorts.IF:
                            interruptManager.Write(position, value);
                            break;

                        #endregion Writes to InterruptManager

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

                        #endregion Writes to Sound

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

                        #endregion Writes to Video

                        default:
                            break;
                    }
                }
                else if (position < 0xFFFF)
                {
                    hram.Write(position, value);
                }
                else
                {
                    interruptManager.Write(position, value);
                }
            }
        }

        #endregion Writes

        public void DMATransferOAM(byte transferDetails)
        {
            // TODO: Implement non-write delay here
            ushort startAddress = (ushort)(transferDetails << 8);
            byte endAddress = 0x00;
            while (endAddress < 0xA0)
            {
                LCD.OAMDMAWrite(endAddress++, Read(startAddress++));
            }
        }

        public override void UpdateTime(int cycles)
        {
            if (cycles <= 0)
                return;
            GlobalTimer.GetInstance().Increment(cycles);
            LCD.UpdateTime(cycles);
            timer.UpdateTime(cycles);
            serial.UpdateTime(cycles);
        }
    }
}