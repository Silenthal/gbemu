namespace GBEmu.Emulator.Graphics
{
    /// <summary>
    /// Contains information about a sprite in the OAM table.
    /// </summary>
    internal struct SpriteInfo
    {
        public int OAMIndex;

        /// <summary>
        /// Represents the X offset of the sprite.
        /// </summary>
        public int XOffset;

        /// <summary>
        /// Represents the Y offset of the sprite.
        /// </summary>
        public int YOffset;

        /// <summary>
        /// Contains the tile's offset in VRAM.
        /// </summary>
        /// <remarks>
        /// If in 8x16 mode, this offset is ANDed with 0xFE0 for the top tile,
        /// and ORed with 0x010 for the bottom tile.
        /// </remarks>
        public int TileOffset;

        /// <summary>
        /// Indicates the tile offset of the upper half of the sprite, in 8 x 16 sprite mode.
        /// </summary>
        public int UpperTileOffset
        {
            get
            {
                return TileOffset & 0xFE0;
            }
        }

        /// <summary>
        /// Indicates the tile offset of the lower half of the sprite, in 8 x 16 sprite mode.
        /// </summary>
        public int LowerTileOffset
        {
            get
            {
                return TileOffset | 0x010;
            }
        }

        /// <summary>
        /// Indicates whether any part of the sprite is visible onscreen.
        /// </summary>
        public bool IsOnScreen
        {
            get
            {
                return XOffset >= 0 && XOffset < 160 && YOffset >= 0 && YOffset < 144;
            }
        }

        /// <summary>
        /// Contains the properties of the sprite. Individual bits can be accessed through the other properties of SpriteInfo.
        /// </summary>
        public int SpriteProperties;

        /// <summary>
        /// Indicates whether the tile for the sprite is flipped horizontally.
        /// </summary>
        public bool XFlip
        {
            get
            {
                return (SpriteProperties & 0x20) != 0;
            }
        }

        /// <summary>
        /// Indicates whether the tile for the sprite is flipped vertically.
        /// </summary>
        public bool YFlip
        {
            get
            {
                return (SpriteProperties & 0x40) != 0;
            }
        }

        /// <summary>
        /// [CGB]Indicates which tile VRAM bank the sprite uses.
        /// </summary>
        public int VRAMBank
        {
            get
            {
                return (SpriteProperties >> 3) & 1;
            }
        }

        /// <summary>
        /// [CGB]Indicates which palette number the sprite uses.
        /// </summary>
        public int CGBPaletteNumber
        {
            get
            {
                return SpriteProperties & 0x7;
            }
        }

        /// <summary>
        /// Indicates which object palette the sprite uses.
        /// </summary>
        public int DMGObjectPaletteNum
        {
            get
            {
                return (SpriteProperties >> 4) & 1;
            }
        }

        /// <summary>
        /// Indicates whether the sprite is drawn over the background.
        /// </summary>
        public bool PriorityOverBG
        {
            get
            {
                return (SpriteProperties & 0x80) == 0;
            }
        }
    }
}