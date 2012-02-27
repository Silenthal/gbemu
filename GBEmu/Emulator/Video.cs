using System;
using System.Collections;
using System.Collections.Generic;

namespace GBRead.Emulator
{
    public class GBColor
    {
        public static byte[] White = new byte[3]
        {
            0xFF, 
            0xFF, 
            0xFF
        };

        public static byte[] LightGrey = new byte[3]
        {
            0x13, 
            0x13, 
            0x13
        };

        public static byte[] DarkGrey = new byte[3]
        {
            0x06, 
            0x06, 
            0x06
        };

        public static byte[] Black = new byte[3]
        {
            0, 
            0, 
            0
        };

        public static byte[][] Colors = new byte[4][]
        {
            White, 
            LightGrey, 
            DarkGrey, 
            Black
        };
    }
    public enum LCDMode : byte { Mode0, Mode1, Mode2, Mode3 }

    class Video : IODevice
    {
        public int ExecutedFrameCycles { get { return CycleCounter; } }
        public bool IsCGB { get; set; }
        #region Screen constants
        private const int RGB24PixelCount = 3;

        private const int LCDWidth = 160;
        private const int LCDHeight = 144;
        private const int LCDStride = LCDWidth * RGB24PixelCount;
        private const int LCDArraySize = LCDStride * LCDHeight;
        
        private const int BGMapWidth = 256;
        private const int BGMapHeight = 256;
        private const int BGMapStride = BGMapWidth * RGB24PixelCount;
        private const int BGMapArraySize = BGMapStride * BGMapHeight;
        private const int BGMapTileRowHeight = TileHeight * BGMapStride;

        private const int TileWidth = 8;
        private const int TileHeight = 8;
        private const int TileStride = TileWidth * RGB24PixelCount;
        private const int TileArraySize = TileStride * TileHeight;
        
        private const int BGMapTileXCount = BGMapWidth / TileWidth;
        private const int BGMapTileYCount = BGMapHeight / TileHeight;
        #endregion

        private const byte LYLimit = 154;

        #region LCDC Constants
        private const byte LCDC_DISPLAY = 0x80;
        private const byte LCDC_WIN_TILE_MAP = 0x40;
        private const byte LCDC_WIN_ENABLE = 0x20;
        private const byte LCDC_BGWIN_TILE_DATA = 0x10;
        private const byte LCDC_BG_TILE_MAP = 0x08;
        private const byte LCDC_SPRITE_SIZE = 0x04;
        private const byte LCD_SPRITE_DISPLAY = 0x02;
        private const byte LCDC_BG_DISPLAY_GB = 0x01;
        #endregion

        #region TileMapOffset
        int[] tileMapOffset2Comp = new int[0x100]
        {
            0x80, 
            0x81, 
            0x82, 
            0x83, 
            0x84, 
            0x85, 
            0x86, 
            0x87, 
            0x88, 
            0x89, 
            0x8A, 
            0x8B, 
            0x8C, 
            0x8D, 
            0x8E, 
            0x9F, 
            0x90, 
            0x91, 
            0x92, 
            0x93, 
            0x94, 
            0x95, 
            0x96, 
            0x97, 
            0x98, 
            0x99, 
            0x9A, 
            0x9B, 
            0x9C, 
            0x9D, 
            0x9E, 
            0x9F, 
            0xA0, 
            0xA1, 
            0xA2, 
            0xA3, 
            0xA4, 
            0xA5, 
            0xA6, 
            0xA7, 
            0xA8, 
            0xA9, 
            0xAA, 
            0xAB, 
            0xAC, 
            0xAD, 
            0xAE, 
            0xAF, 
            0xB0, 
            0xB1, 
            0xB2, 
            0xB3, 
            0xB4, 
            0xB5, 
            0xB6, 
            0xB7, 
            0xB8, 
            0xB9, 
            0xBA, 
            0xBB, 
            0xBC, 
            0xBD, 
            0xBE, 
            0xBF, 
            0xC0, 
            0xC1, 
            0xC2, 
            0xC3, 
            0xC4, 
            0xC5, 
            0xC6, 
            0xC7, 
            0xC8, 
            0xC9, 
            0xCA, 
            0xCB, 
            0xCC, 
            0xCD, 
            0xCE, 
            0xCF, 
            0xD0, 
            0xD1, 
            0xD2, 
            0xD3, 
            0xD4, 
            0xD5, 
            0xD6, 
            0xD7, 
            0xD8, 
            0xD9, 
            0xDA, 
            0xDB, 
            0xDC, 
            0xDD, 
            0xDE, 
            0xDF, 
            0xE0, 
            0xE1, 
            0xE2, 
            0xE3, 
            0xE4, 
            0xE5, 
            0xE6, 
            0xE7, 
            0xE8, 
            0xE9, 
            0xEA, 
            0xEB, 
            0xEC, 
            0xED, 
            0xEE, 
            0xEF, 
            0xF0, 
            0xF1, 
            0xF2, 
            0xF3, 
            0xF4, 
            0xF5, 
            0xF6, 
            0xF7, 
            0xF8, 
            0xF9, 
            0xFA, 
            0xFB, 
            0xFC, 
            0xFD, 
            0xFE, 
            0xFF, 
            0x00, 
            0x01, 
            0x02, 
            0x03, 
            0x04, 
            0x05, 
            0x06, 
            0x07, 
            0x08, 
            0x09, 
            0x0A, 
            0x0B, 
            0x0C, 
            0x0D, 
            0x0E, 
            0x0F, 
            0x10, 
            0x11, 
            0x12, 
            0x13, 
            0x14, 
            0x15, 
            0x16, 
            0x17, 
            0x18, 
            0x19, 
            0x1A, 
            0x1B, 
            0x1C, 
            0x1D, 
            0x1E, 
            0x1F, 
            0x20, 
            0x21, 
            0x22, 
            0x23, 
            0x24, 
            0x25, 
            0x26, 
            0x27, 
            0x28, 
            0x29, 
            0x2A, 
            0x2B, 
            0x2C, 
            0x2D, 
            0x2E, 
            0x2F, 
            0x30, 
            0x31, 
            0x32, 
            0x33, 
            0x34, 
            0x35, 
            0x36, 
            0x37, 
            0x38, 
            0x39, 
            0x3A, 
            0x3B, 
            0x3C, 
            0x3D, 
            0x3E, 
            0x3F, 
            0x40, 
            0x41, 
            0x42, 
            0x43, 
            0x44, 
            0x45, 
            0x46, 
            0x47, 
            0x48, 
            0x49, 
            0x4A, 
            0x4B, 
            0x4C, 
            0x4D, 
            0x4E, 
            0x4F, 
            0x50, 
            0x51, 
            0x52, 
            0x53, 
            0x54, 
            0x55, 
            0x56, 
            0x57, 
            0x58, 
            0x59, 
            0x5A, 
            0x5B, 
            0x5C, 
            0x5D, 
            0x5E, 
            0x5F, 
            0x60, 
            0x61, 
            0x62, 
            0x63, 
            0x64, 
            0x65, 
            0x66, 
            0x67, 
            0x68, 
            0x69, 
            0x6A, 
            0x6B, 
            0x6C, 
            0x6D, 
            0x6E, 
            0x6F, 
            0x70, 
            0x71, 
            0x72, 
            0x73, 
            0x74, 
            0x75, 
            0x76, 
            0x77, 
            0x78, 
            0x79, 
            0x7A, 
            0x7B, 
            0x7C, 
            0x7D, 
            0x7E, 
            0x7F 
        };
        #endregion

        private int VramBank = 0;

        private byte LCDControl;//FF40
        #region LCD Control Options
        private int WindowTileMapStart
        {
            get
            {
                return (LCDControl & LCDC_WIN_TILE_MAP) == 0 ? 0x1800 : 0x1C00;
            }
        }//Bit 6
        private int BGTileMapDisplayStart
        {
            get
            {
                return (LCDControl & LCDC_BG_TILE_MAP) == 0 ? 0x1800 : 0x1C00;
            }
        }//Bit 3
        private bool isLCDEnabled
        {
            get
            {
                return (LCDControl & LCDC_DISPLAY) != 0;
            }
        }//Bit 7
        private bool IsWindowEnabled
        {
            get
            {
                return (LCDControl & LCDC_WIN_ENABLE) != 0;
            }
        }//Bit 5
        private int BGWinTileDataStart
        {
            get
            {
                return isComplementTileIndexingUsed ? 0x800 : 0;
            }
        }//Bit 4
        private int SpriteHeight
        {
            get
            {
                return (LCDControl & LCDC_SPRITE_SIZE) == 0 ? 8 : 16;
            }

        }//Bit 2
        private bool areSpritesDisplayed
        {
            get
            {
                return (LCDControl & LCD_SPRITE_DISPLAY) != 0;
            }
        }//Bit 1
        private bool isBackgroundEnabled
        {
            get
            {
                return (LCDControl & LCDC_BG_DISPLAY_GB) != 0;
            }
        }//Bit 0
        private bool isComplementTileIndexingUsed
        {
            get
            {
                return (LCDControl & LCDC_BGWIN_TILE_DATA) == 0;
            }
        }
        #endregion
        private byte stat;
        public byte LCDStatus
        {
            get
            {
                return (byte)(stat | LYCoincidence | LCDMode);
            }
            set
            {
                stat = (byte)(value & 0x78);
            }
        }//FF41
        #region LCD Status options
        private bool LYCCoincidenceInterruptEnabled
        {
            get
            {
                return (LCDStatus & STAT_LYC_LY_INTERRUPT_FLAG) != 0;
            }
        }
        private bool Mode2_OAMInterruptEnabled
        {
            get
            {
                return (LCDStatus & STAT_MODE2_OAM_INTERRUPT) != 0;
            }
        }
        private bool Mode1_VBlankInterruptEnabled
        {
            get
            {
                return (LCDStatus & STAT_MODE1_VBLANK_INTERRUPT) != 0;
            }
        }
        private bool Mode0_HBlankInterruptEnabled
        {
            get
            {
                return (LCDStatus & STAT_MODE0_HBLANK_INTERRUPT) != 0;
            }
        }
        private byte LYCoincidence
        {
            get
            {
                return LY == LYCompare ? (byte)0x4 : (byte)0;
            }
        }
        #endregion
        private byte LCDMode;
        private bool oamAccessAllowed
        {
            get
            {
                return (LCDMode & 0x2) == 0;
            }
        }
        private bool vramAccessAllowed
        {
            get
            {
                return LCDMode != Mode3;
            }
        }

        private byte ScrollX;//FF43
        private byte ScrollY;//FF42
        private byte WindowX;//FF4B
        private byte WindowY;//FF4A
        private byte LYCompare;//FF45
        private byte LY;//FF44

        private byte BackgroundPaletteData;
        private byte[][] BGPalette_DMG;
        private byte ObjectPalette0Data;
        private byte[][] OBJPalette0_DMG;
        private byte ObjectPalette1Data;
        private byte[][] OBJPalette1_DMG;

        #region STAT Constants
        #region Enable Constants (OR)
        private const byte STAT_LYC_LY_INTERRUPT_FLAG = 0x40;
        private const byte STAT_MODE2_OAM_INTERRUPT = 0x20;
        private const byte STAT_MODE1_VBLANK_INTERRUPT = 0x10;
        private const byte STAT_MODE0_HBLANK_INTERRUPT = 0x08;
        private const byte STAT_COINCIDENCE_UNEQUAL = 0x04;
        #endregion


        #region LCD Mode Constants
        private const byte STAT_LCD_MODE_CLEAR = 0xFC;
        private const byte STAT_LCD_MODE_0 = 0x0;
        private const byte STAT_LCD_MODE_1 = 0x1;
        private const byte STAT_LCD_MODE_2 = 0x2;
        private const byte STAT_LCD_MODE_3 = 0x3;
        #endregion
        #endregion

        #region Cycle Constants
        private int LineCounter;
        private const int Mode0 = 0;
        private const int Mode1 = 1;
        private const int Mode2 = 2;
        private const int Mode3 = 3;
        private const int Mode1Cycles = 4560;
        private const int Mode2Cycles = 80;
        private const int Mode3Cycles = 172;
        private const int Mode0Cycles = 204;
        private const int LYOnScreenCycles = 65664;
        private const int LCDDrawCycles = 70224;
        private const int LineDrawCycles = 456;
        #endregion

        public byte[] LCDMap { get; private set; }

        public byte[][] VRAM;//0x8000-0x9FFF, x2 for GBC
        
        public byte[] OAM;

        private byte[][] BGMap;
        private int[][] BGMapUsedTileNum;
        private bool[][] TileMapChanged;
        private int BGSelectedMap
        {
            get
            {
                return BGTileMapDisplayStart == 0x1800 ? 0 : 1;
            }
        }

        private bool VBlankInterruptRequest = false;
        public byte VBlankInterrupt 
        { 
            get
            {
                if (VBlankInterruptRequest)
                {
                    VBlankInterruptRequest = false;
                    return 0x1;
                }
                else return 0;
            }
        }

        private bool LCDCInterruptRequest = false;
        public byte LCDCInterrupt 
        {
            get
            {
                if (LCDCInterruptRequest)
                {
                    LCDCInterruptRequest = false;
                    return 0x2;
                }
                else return 0;
            }
        }

        private bool LCDScreenOn;

        private int DMACounter;

        public bool DMATransferRequest = false;

        public Video()
        {
            VramBank = 0;
            LCDStatus = 0;
            LCDControl = 0;
            //LCD Map is what's actually going to be shown on the screen.
            //160x144x3 bytes, RGB 24-bit
            LCDMap = new byte[LCDStride * LCDHeight];

            //Tile Map from $8000-$97FF
            //LCD Map from $9800-9FFF
            VRAM = new byte[2][];
            VRAM[0] = new byte[0x2000];
            VRAM[1] = new byte[0x2000];

            OAM = new byte[0xA0];

            LCDMode = Mode2;

            BGPalette_DMG = new byte[4][];
            for (int i = 0; i < 4; i++)
            {
                BGPalette_DMG[i] = new byte[3];
            }

            OBJPalette0_DMG = new byte[4][];
            for (int i = 0; i < 4; i++)
            {
                OBJPalette0_DMG[i] = new byte[3];
            }

            OBJPalette1_DMG = new byte[4][];
            for (int i = 0; i < 4; i++)
            {
                OBJPalette1_DMG[i] = new byte[3];
            }

            IsCGB = false;

            LCDScreenOn = true;

            DMACounter = 0;

            BGMap = new byte[2][];
            BGMap[0] = new byte[BGMapStride * BGMapHeight];
            BGMap[1] = new byte[BGMapStride * BGMapHeight];
            BGMapUsedTileNum = new int[2][];
            BGMapUsedTileNum[0] = new int[32 * 32];
            BGMapUsedTileNum[1] = new int[32 * 32];

            TileMapChanged = new bool[2][];
            TileMapChanged[0] = new bool[384];
            TileMapChanged[1] = new bool[384];
            for (int i = 0; i < 384; i++)
            {
                TileMapChanged[0][i] = false;
                TileMapChanged[1][i] = false;
            }
        }

        //Rules for reading:
        //Just return the values in the appropriate memory locations
        public override byte Read(int position)
        {
            if (position < 0xA000)
            {
                if (LCDScreenOn) return vramAccessAllowed ? VRAM[VramBank][position - 0x8000] : (byte)0xFF;
                else return VRAM[VramBank][position - 0x8000];
            }
            else if (position > 0xFDFF && position < 0xFEA0)
            {
                if (LCDScreenOn) return oamAccessAllowed ? OAM[position - 0xFE00] : (byte)0xFF;
                else return OAM[position - 0xFE00];
            }
            else switch (position & 0xFF)
                {
                    case MMU.LCDC:
                        return LCDControl;
                    case MMU.STAT:
                        return LCDStatus;
                    case MMU.SCX:
                        return ScrollX;
                    case MMU.SCY:
                        return ScrollY;
                    case MMU.LY:
                        return LY;
                    case MMU.LYC:
                        return LYCompare;
                    case MMU.BGP:
                        return BackgroundPaletteData;
                    case MMU.OBP0:
                        return ObjectPalette0Data;
                    case MMU.OBP1:
                        return ObjectPalette1Data;
                    case MMU.WX:
                        return WindowX;
                    case MMU.WY:
                        return WindowY;
                    case MMU.VBK:
                        return (byte)VramBank;
                    case MMU.BCPD:
                    case MMU.BCPS:
                    case MMU.OCPS:
                    case MMU.OCPD:
                    case MMU.DMA:
                    case MMU.KEY1:
                    case MMU.HDMA1:
                    case MMU.HDMA2:
                    case MMU.HDMA3:
                    case MMU.HDMA4:
                    case MMU.HDMA5:
                    case MMU.RP:
                    case MMU.SVBK:
                        return 0;
                

                }
            return 0;
        }

        //Rules for writing:
        //Whenever a write is made to a tile location, the code should check whether that
        //tile is in use. If it is, do a function to write the tile line to the background map.
        //If it's to an OAM sprite, then do the same.
        public override void Write(int position, byte data)
        {
            if (position < 0xA000)
            {
                if (!LCDScreenOn || (LCDScreenOn && vramAccessAllowed))
                {
                    VRAM[VramBank][position - 0x8000] = data;
                    if (position < 9800)
                    {
                        TileMapChanged[VramBank][(position - 0x8000) >> 4] = true;
                    }
                }
            }
            else if (position > 0xFDFF && position < 0xFEA0)
            {
                if (!LCDScreenOn || (LCDScreenOn && oamAccessAllowed))
                {
                    OAM[position - 0xFE00] = data;
                }
            }
            else if (position >= 0xFF40)
            {
                switch (position & 0xFF)
                {
                    case MMU.LCDC:
                        LCDControl = data;
                        CheckLCDCStatus();
                        break;
                    case MMU.STAT:
                        LCDStatus = data;
                        break;
                    case MMU.SCX:
                        ScrollX = data;
                        break;
                    case MMU.SCY:
                        ScrollY = data;
                        break;
                    case MMU.LYC:
                        LYCompare = data;
                        break;
                    case MMU.BGP:
                        BackgroundPaletteData = data;
                        UpdateGBBackgroundPalette();
                        break;
                    case MMU.OBP0:
                        ObjectPalette0Data = data;
                        UpdateObjectPalette0DMG();
                        break;
                    case MMU.OBP1:
                        ObjectPalette1Data = data;
                        UpdateObjectPalette1DMG();
                        break;
                    case MMU.WX:
                        WindowX = data;
                        break;
                    case MMU.WY:
                        WindowY = data;
                        break;
                    case MMU.VBK:
                        //VramBank = data & 1;
                        break;
                    case MMU.BCPD:
                    case MMU.BCPS:
                    case MMU.OCPS:
                    case MMU.OCPD:
                    case MMU.DMA:
                        DMATransferRequest = true;
                        DMACounter = 200;
                        break;
                    case MMU.KEY1:
                    case MMU.HDMA1:
                    case MMU.HDMA2:
                    case MMU.HDMA3:
                    case MMU.HDMA4:
                    case MMU.HDMA5:
                    case MMU.RP:
                    case MMU.SVBK:
                        break;
                }
            }
        }

        private void UpdateBackgroundMapGB()
        {
            //Iterate over each tile in the 'BG Map'.
            //The BG Map is a 32 by 32 tile area, 256x256 pixels.
            //In the updating part, only the BG Map currently on the display is updated.
            //For each tile:
            //-Check the index num at location (9800/9C00) + i
            //-If the corresponding location matches the loc in cache
            //--If location shows as 'not changed', do no copy.
            //-Else, copy new tile.
            for (int i = 0; i < (32 * 32); i++)
            {
                byte preIndex = VRAM[0][BGTileMapDisplayStart];
                if (isComplementTileIndexingUsed) preIndex += 0x80;
                int tileLocation = BGWinTileDataStart + (preIndex * 0x10);
                if ((BGMapUsedTileNum[BGSelectedMap][i] != tileLocation)
                    || TileMapChanged[0][(tileLocation >> 4) - 0x100])
                {
                    CopyTileGB(i, tileLocation);
                }
            }
        }

        private void CopyTileGB(int BGTileNum, int TileDataLocation)
        {
            //Each row is 32 tiles long.
            //The tile row is represented by (BGTileNum >> 5) * (8 * BGMapStride)
            //The tile column is represented as (BGTileNum & 0x1F) * (8 * RGB24PixelCount)
            //Add those two to get the start of the specific tile in the map.
            //Each line in the map is accessed by adding (i * BGMapStride) to it.
            int BGMapTileLocation = ((BGTileNum >> 5) * BGMapTileRowHeight) + ((BGTileNum & 0x1F) * TileStride);
            for (int y = 0; y < 8; y++)
            {
                int BGMapLineStart = BGMapTileLocation + (y * BGMapStride);
                int tileDataLineStart = TileDataLocation + (y * 2);
                byte line0 = VRAM[0][tileDataLineStart];
                byte line1 = VRAM[0][tileDataLineStart + 1];
                for (int x = 7; x >= 0; x--)
                {
                    int colorNum = ((line0 >> x) & 1) + (((line1 >> x) & 1) << 1);
                    BGMap[BGSelectedMap][BGMapLineStart + 0] = BGPalette_DMG[colorNum][0];
                    BGMap[BGSelectedMap][BGMapLineStart + 1] = BGPalette_DMG[colorNum][1];
                    BGMap[BGSelectedMap][BGMapLineStart + 2] = BGPalette_DMG[colorNum][2];
                }
            }
            TileMapChanged[0][(TileDataLocation >> 4) - 0x100] = false;//Set it to false when tile data is read.
        }

        private void CheckLCDCStatus()
        {
            if ((LCDControl & 0x80) == 0)
            {
                if (LCDScreenOn)
                {
                    LCDScreenOn = false;
                }
            }
            else
            {
                if (!LCDScreenOn)
                {
                    LCDScreenOn = true;
                    CycleCounter = 0;
                    LineCounter = 0;
                    LY = 0;
                    LCDMode = Mode2;
                }
            }
        }

        public void UpdateGBBackgroundPalette()
        {
            for (int i = 0; i < 4; i++)
            {
                int BGPIndex = (BackgroundPaletteData >> (i * 2)) & 0x03; //This contains the shade for Color i
                Array.Copy(GBColor.Colors[BGPIndex], BGPalette_DMG[i], RGB24PixelCount);
            }
        }

        public void UpdateObjectPalette0DMG()
        {
            for (int i = 1; i < 4; i++)//Only copy for colors 1-3, because 0 is transparent
            {
                int OBJPIndex = (ObjectPalette0Data >> (i * 2)) & 0x03; //This contains the shade for Color i
                Array.Copy(GBColor.Colors[OBJPIndex], OBJPalette0_DMG[i], RGB24PixelCount);
            }
        }

        public void UpdateObjectPalette1DMG()
        {
            for (int i = 1; i < 4; i++)//Only copy for colors 1-3, because 0 is transparent
            {
                int OBJPIndex = (ObjectPalette1Data >> (i * 2)) & 0x03; //This contains the shade for Color i
                Array.Copy(GBColor.Colors[OBJPIndex], OBJPalette1_DMG[i], RGB24PixelCount);
            }
        }

        private int TileLinesToDMGColorIndex(int lineAddr, int lineOffset, int pixelNumFromLeft)
        {
            int pix0 = VRAM[0][lineAddr + (lineOffset * 2)]  >> (7 - pixelNumFromLeft) & 1;
            int pix1 = (VRAM[0][lineAddr + (lineOffset * 2) + 1] >> (7 - pixelNumFromLeft) << 1) & 2;
            return pix0 + pix1;
        }

        /// <summary>
        /// Used for drawing a scanline in GB mode.
        /// Each call to this function will increment LY by 1, looping at 154.
        /// </summary>
        private void DrawDMGScanline()
        {
            if (!LCDScreenOn)
            {
                if (LY == 70)
                {
                    for (int i = 0; i < LCDWidth; i++)
                    {
                        LCDMap[i + 0] = 0;
                        LCDMap[i + 1] = 0;
                        LCDMap[i + 2] = 0;
                    }
                }
                else
                {
                    for (int i = 0; i < LCDWidth; i++)
                    {
                        LCDMap[i + 0] = 255;
                        LCDMap[i + 1] = 255;
                        LCDMap[i + 2] = 255;
                    }
                }
                return;
            }
            //Drawing a scanline...
            //Palettes for the tiles wil use the BGPalette_DMG
            //Each entry in the DMG palette stands for a color, in RGB24 format.
            //Drawing will occur from VRAM. In the case of DMG mode, tiles reside in one bank.
            //Drawing method:
            //-Find the start of the line. This depends on the ScrollX and ScrollY registers.
            //-Then, for each 'pixel' on the line (160), do:
            //--The tile it refers to
            //--The specific lines in the tile
            //--The specific sections of the two bytes composing the line, in the time
            //--Conversion info for those bytes (could do a method call here)
            //--Copying the color info for the pixel into the line.
            if (LY >= LCDHeight)
            {
                return;
            }
            //First, the Y-position of the pixel in the BG Map.
            byte pixelY = (byte)(ScrollY + LY);
            int BGMapLineStart = BGMap[BGSelectedMap][pixelY * BGMapStride];
            for (int i = 0; i < 160; i++)
            {
                byte pixelX = (byte)(ScrollX + i);
                int BGMapXStart = BGMapLineStart + (pixelX * RGB24PixelCount);
                int pixelPosInLCD = (pixelY * LCDStride) + (pixelX * RGB24PixelCount);
                LCDMap[pixelPosInLCD + 0] = BGMap[BGSelectedMap][BGMapXStart + 0];
                LCDMap[pixelPosInLCD + 1] = BGMap[BGSelectedMap][BGMapXStart + 1];
                LCDMap[pixelPosInLCD + 2] = BGMap[BGSelectedMap][BGMapXStart + 2];
            }
        }

        private void DrawDMGSpriteline()
        {
            //Drawing a spriteline...
            //Palettes for the sprites wil use the _________
            //Each entry in the OBJ palette stands for a color, in RGB24 format.
            //Drawing will occur from VRAM. In the case of DMG mode, tiles reside in one bank.
            //Drawing method:
            //-Find out which palettes lie on the line. In DMG mode, sprites that lie on the same
            //-scanline get blah blah...
            //-Gonna do that part after.

            //-Find the start of the line. This depends on the ScrollX and ScrollY registers.
            //-Then, for each 'pixel' on the line (160), do:
            //--The tile it refers to
            //--The specific lines in the tile
            //--The specific sections of the two bytes composing the line, in the time
            //--Conversion info for those bytes (could do a method call here)
            //--Copying the color info for the pixel into the line.
            if (LY >= LCDHeight)
            {
                return;
            }
            //First, the Y-position of the pixel in the BG Map.
            byte pixelY = (byte)(ScrollY + LY);
            //Then, for each pixel on the line...
            for (int i = 0; i < 160; i++)
            {
                //Then the X position of the pixel in the BG Map.
                byte pixelX = (byte)(ScrollX + i);

                //Then, the address of the tile the pixel lies in.
                //The tile map is 32 x 32 tiles, so each row is 32 tiles long.
                int vramBGMAddr = ((pixelY >> 3) * BGMapTileXCount) + (pixelX >> 3) + 0x1800;
                int tileIndex = VRAM[0][vramBGMAddr];

                //Tile index will be different depending on the indexing method.
                if (BGWinTileDataStart != 0) tileIndex = ((tileIndex + 0x80) & 0xFF);
                tileIndex *= 10; //This adjusts it so it points to the start of a specific tile.

                //Then get the number of the color in the palette.
                int palNum = TileLinesToDMGColorIndex(tileIndex, pixelY & 3, pixelX & 3);
                //And the pixel's position in the LCD map, which is actually what's going to be shown.
                int pixelPosInLCD = (pixelY * LCDStride) + (pixelX * RGB24PixelCount);

                //Then, draw each color.
                LCDMap[pixelPosInLCD + 0] = BGPalette_DMG[palNum][0];
                LCDMap[pixelPosInLCD + 1] = BGPalette_DMG[palNum][1];
                LCDMap[pixelPosInLCD + 2] = BGPalette_DMG[palNum][2];
            }
        }

        private Dictionary<byte, byte> GetSpritesOnLine()
        {
            //Return a list of sprite indexes
            Dictionary<byte, byte> returned = new Dictionary<byte, byte>();
            return returned;
        }

        public override void UpdateCounter(int cycles)
        {
            if (LCDScreenOn)
            {
                CycleCounter += cycles;
                LineCounter += cycles;
                if (DMACounter > 0)
                {
                    DMACounter -= cycles;
                    if (DMACounter <= 0) DMATransferRequest = false;
                }
                #region Changing modes (need to add special case for drawing scanline 0)
                //Modes go 2 -> 3 -> 0 -> 2 ->...0 -> 1 -> 2
                //LY increases happen after 0, or after each line of 1.
                switch (LCDMode)
                {
                    case Mode2://Mode 2: Searching OAM...no access to OAM, progresses to 3
                        if (LineCounter >= Mode2Cycles)
                        {
                            LineCounter -= Mode2Cycles;
                            //When switching to 3, write the current scanline.
                            LCDMode = Mode3;
                            UpdateBackgroundMapGB();
                            DrawDMGScanline();
                        }
                        break;
                    case Mode3://Mode 3: Searching OAM/VRAM...no access to OAM/VRAM or Palette Data, progresses to 0
                        if (LineCounter >= Mode3Cycles)
                        {
                            //When switching to mode 0, the line is already drawn, so do nothing.
                            LCDMode = Mode0;
                            LineCounter -= Mode3Cycles;
                            LCDCInterruptRequest = Mode0_HBlankInterruptEnabled || (LYCCoincidenceInterruptEnabled && (LYCoincidence != 0));
                        }
                        break;
                    case Mode0://Mode 0: HBlank...Access allowed, progresses to 1 (if at end of screen draw), or 2.
                        if (LineCounter >= Mode0Cycles)
                        {
                            LineCounter -= Mode0Cycles;
                            IncrementLY();
                            if (LY >= LCDHeight)
                            {
                                LCDMode = Mode1;
                                LCDCInterruptRequest = Mode1_VBlankInterruptEnabled;
                                VBlankInterruptRequest = true;
                            }
                            else
                            {
                                LCDMode = Mode2;
                            }
                        }
                        break;
                    case Mode1://Mode 1: VBlank...access allowed, progresses to 2
                        if (LineCounter >= LineDrawCycles)
                        {
                            LineCounter -= LineDrawCycles;
                            IncrementLY();
                            if (LY == 0)
                            {
                                CycleCounter -= LCDDrawCycles;
                                LCDMode = Mode2;
                            }
                        }
                        DrawDMGScanline();
                        break;
                }
                #endregion
            }
            else
            {
                LY = 0;
                for (int i = 0; i < LCDHeight; i++)
                {
                    DrawDMGScanline();
                    LY++;
                }
            }
        }

        private void IncrementLY()
        {
            LY++;
            if (LY >= LYLimit) LY = 0;
            if (LY == LYCompare && LYCCoincidenceInterruptEnabled) LCDCInterruptRequest = true;
        }


    }
}
