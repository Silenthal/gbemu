namespace GBEmu.Emulator.Cartridge
{
    public class MBC1 : Cart
    {
        /// <summary>
        /// The current mode of the MBC1, which determines how the RAM bank is used in ROM/RAM access.
        /// </summary>
        /// <remarks>
        /// The MBC1 has two registers for accessing different banks - BANK1 and BANK2.
        /// When the mode register is off, BANK1 represents the bottom 5 bits of the bank, and BANK2
        /// represents the top 2. These two are used for memory access in the 0x4000-0x7FFF range.
        ///
        /// When the mode is on, then BANK2 is also used for accesses in the 0x0000-0x3FFF range,
        /// while BANK1 remains 0.
        ///
        /// For multicart MBC1, everything is the same except BANK1 represents the bottom 4 bits
        /// of the bank instead.
        /// </remarks>
        private bool Mode;

        private bool IsMulticart;

        public MBC1(byte[] inFile, CartFeatures cartFeatures)
            : base(inFile, cartFeatures)
        {
            Mode = false;
            IsMulticart = CheckMulticart();
        }

        /// <summary>
        /// Checks for multicart MBC1 using a heuristic.
        /// </summary>
        /// <returns></returns>
        private bool CheckMulticart()
        {
            // For multicart games, BANK1 represents the bottom 4 bits of the bank, and BANK2
            // the top 2 - games should be able to go up to BANK2 = 3
            if (romFile.Length >> (14 + 4) < 4)
            {
                return false;
            }
            byte[] logo = new byte[]
            {
                0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D,
                0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99,
                0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E
            };
            int logoCount = 0;
            // For each possible value of BANK2, count logos that appear since
            // each game should have a regular GB header
            for (int bank2 = 0; bank2 < 4; bank2++)
            {
                var logo_index = (bank2 << (14 + 4)) + HeaderConstants.LogoOffset;
                bool isLogo = true;
                for (int j = 0; j < logo.Length; j++)
                {
                    if (romFile[logo_index + j] != logo[j])
                    {
                        isLogo = false;
                    }
                }
                if (isLogo)
                {
                    logoCount++;
                }
            }
            // Minimum 2 logos + the logo of the menu
            return logoCount > 2;
        }

        public override byte Read(int position)
        {
            if (position >= 0 && position < 0x8000)
            {
                int addrLow = position & 0x3FFF;
                int bank;
                if (position < 0x4000)
                {
                    if (Mode)
                    {
                        bank = IsMulticart ? CartRamBank << 4 : CartRamBank << 5;
                    }
                    else
                    {
                        bank = 0;
                    }
                }
                else
                {
                    bank = IsMulticart ? ((CartRamBank << 4) | (RomBank & 0xF)) : ((CartRamBank << 5) | (RomBank & 0x1F));
                }
                return romFile[(bank << 14 | addrLow) % romFile.Length];
            }
            else
            {
                return base.Read(position);
            }
        }

        protected override void MBCWrite(int position, byte value)
        {
            switch (position >> 13)
            {
                case 0://0x0000 - 0x1FFF
                    RamEnabled = (value & 0xF) == 0xA;
                    break;

                case 1://0x2000 - 0x3FFF
                    {
                        int writeVal = value & 0x1F;
                        if (writeVal == 0)
                        {
                            writeVal++;
                        }
                        RomBank = writeVal;
                    }
                    break;

                case 2://0x4000 - 0x5FFF
                    {
                        int writeVal = value & 0x03;
                        CartRamBank = writeVal;
                    }
                    break;

                case 3://0x6000 - 0x7FFF
                    Mode = ((value & 1) != 0);
                    break;
            }
        }

        protected override byte CartRamRead(int position)
        {
            position -= 0xA000;
            if (RamEnabled)
            {
                if (Mode)
                {
                    return CartRam[((CartRamBank << 13) | position) % CartRam.Length];
                }
                else
                {
                    return CartRam[position % CartRam.Length];
                }
            }
            else
            {
                return 0xFF;
            }
        }

        protected override void CartRamWrite(int position, byte value)
        {
            position -= 0xA000;
            if (RamEnabled)
            {
                if (Mode)
                {
                    CartRam[((CartRamBank << 13) | position) % CartRam.Length] = value;
                }
                else
                {
                    CartRam[position % CartRam.Length] = value;
                }
            }
        }
    }
}