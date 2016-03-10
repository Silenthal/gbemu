namespace GBEmu.Emulator.Cartridge
{
    /// <summary>
    /// Represents a type of cartridge, specified in the cartridge header.
    /// </summary>
    public enum CartridgeType : byte
    {
        ROM = 0x00,

        MBC1 = 0x01,
        MBC1_R = 0x02,
        MBC1_RB = 0x03,

        MBC2 = 0x05,
        MBC2_B = 0x06,

        ROM_R = 0x08,
        ROM_RB = 0x09,

        // MMM01		= 0x0B,
        // MMM01_R		= 0x0C,
        // MMM01_RB	= 0x0D,

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

        //PC			= 0xFC,
        //TAMA5		= 0xFD,
        //HuC3		= 0xFE,
        //HuC1_RB		= 0xFF
    }
}