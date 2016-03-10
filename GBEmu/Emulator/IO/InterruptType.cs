using System;

namespace GBEmu.Emulator.IO
{
    [Flags]
    public enum InterruptType : byte
    {
        None = 0,
        VBlank = 0x1,
        LCDC = 0x2,
        Timer = 0x4,
        Serial = 0x8,
        Joypad = 0x10
    }
}