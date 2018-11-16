namespace GBEmu.Emulator.Cartridge
{
    public static class HeaderConstants
    {
        public const int LogoOffset = 0x104;
        public const int TitleOffset = 0x134;
        public const int GbcManufacturerCode = 0x13F;
        public const int CartTypeOffset = 0x143;
        public const int NewLicenseCodeOff_1 = 0x144;
        public const int NewLicenseCodeOff_2 = 0x145;
        public const int SuperGBSupportOffset = 0x146;
        public const int RomTypeOffset = 0x147;
        public const int RomSizeOffset = 0x148;
        public const int RamSizeOffset = 0x149;
        public const int CountryCodeOffset = 0x14A;
        public const int OldLicenseCode = 0x14B;
        public const int VersionOffset = 0x14C;
        public const int ComplementOffset = 0x14D;
        public const int ChecksumOffHi = 0x14E;
        public const int ChecksumOffLo = 0x14F;
    }
}