using System;

namespace GBEmu.Emulator.Cartridge
{
    /// <summary>
    /// Represents features present in the cart.
    /// </summary>
    [Flags]
    public enum CartFeatures : byte
    {
        None = 0x0,
        RAM = 0x1,
        BatteryBacked = 0x2,
        Timer = 0x4,
        Rumble = 0x8,
    }
}