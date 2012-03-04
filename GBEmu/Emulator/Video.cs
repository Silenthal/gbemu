using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using GBEmu.Render;

namespace GBEmu.Emulator
{
	public class GBColor
	{
		#region Predefined DMG Colors
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
		#endregion

		public static byte[][] Colors = new byte[4][]
		{
			White, 
			LightGrey, 
			DarkGrey, 
			Black
		};
	}
	public enum LCDMode : byte { Mode0, Mode1, Mode2, Mode3 }

	class Video : TimedIODevice
	{
		private InterruptManager interruptManager;
		private IRenderable screen;
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
		
		private const int TileMapXCount = BGMapWidth / TileWidth;
		private const int TileMapYCount = BGMapHeight / TileHeight;
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

		#region Sprite Read Constants
		private const byte SpritePalette = 0x07;	//_____NNN : Selects from OBP 0-7 (CGB)
		private const byte SpriteVRamBank = 0x08;	//____N___ : Selects from VRAM Bank 0 or 1 (CGB)
		private const byte SpritePaletteNum = 0x10;	//___N____ : Selects from OBP0 or OBP1
		private const byte SpriteXFlip = 0x20;		//__N_____ : 1 = Horizontal Flip
		private const byte SpriteYFlip = 0x40;		//_N______ : 1 = Vertical Flip
		private const byte SpritePriority = 0x80;	//N_______ : 1 = (hides behind BG color 1 - 3)
		#endregion

		private int VramBank = 0;

		private byte LCDControl;//FF40
		#region LCD Control Options
		private bool LCDEnabled
		{
			get
			{
				return (LCDControl & LCDC_DISPLAY) != 0;
			}
		}//Bit 7
		private int WindowTileMapStart
		{
			get
			{
				return (LCDControl & LCDC_WIN_TILE_MAP) == 0 ? 0x1800 : 0x1C00;
			}
		}//Bit 6
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
		private int BGTileMapStart
		{
			get
			{
				return (LCDControl & LCDC_BG_TILE_MAP) == 0 ? 0x1800 : 0x1C00;
			}
		}//Bit 3
		private int SpriteHeight
		{
			get
			{
				return (LCDControl & LCDC_SPRITE_SIZE) == 0 ? 8 : 16;
			}

		}//Bit 2
		private bool SpritesEnabled
		{
			get
			{
				return (LCDControl & LCD_SPRITE_DISPLAY) != 0;
			}
		}//Bit 1
		private bool DMGBackgroundEnabled
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
				return (byte)(stat | LYCoincidence | (byte)LCDMode);
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
		private LCDMode LCDMode;
		private bool oamAccessAllowed
		{
			get
			{
				return (LCDMode & Emulator.LCDMode.Mode2) == 0;
			}
		}
		private bool vramAccessAllowed
		{
			get
			{
				return LCDMode != LCDMode.Mode3;
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
		#endregion

		#region Cycle Constants
		private int LineCounter;
		private const int Mode1Cycles = 4560;
		private const int Mode2Cycles = 80;
		private const int Mode3Cycles = 172;
		private const int Mode0Cycles = 204;
		private const int LYOnScreenCycles = 65664;
		private const int LCDDrawCycles = 70224;
		private const int LineDrawCycles = 456;
		#endregion

		public byte[] LCDMap { get; private set; }

		public byte[,] VRAM;//0x8000-0x9FFF, x2 for GBC
		
		public byte[] OAM;

		private bool LCDScreenOn;

		private int DMACounter;

		public bool DMATransferRequest = false;

		private SpriteRef[] SpriteTable;

		public Video(InterruptManager iM, IRenderable newScreen)
		{
			screen = newScreen;
			interruptManager = iM;
			SpriteTable = new SpriteRef[40];
			VramBank = 0;
			LCDStatus = 0;
			LCDControl = 0;
			//LCD Map is what's actually going to be shown on the screen.
			//160x144x3 bytes, RGB 24-bit
			LCDMap = new byte[LCDStride * LCDHeight];

			//Tile Map from $8000-$97FF
			//LCD Map from $9800-9FFF
			VRAM = new byte[2, 0x2000];

			OAM = new byte[0xA0];

			LCDMode = LCDMode.Mode2;

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
		}

		//Rules for reading:
		//Just return the values in the appropriate memory locations
		public override byte Read(int position)
		{
			if (position < 0xA000)
			{
				if (LCDScreenOn) return vramAccessAllowed ? VRAM[VramBank, position - 0x8000] : (byte)0xFF;
				else return VRAM[VramBank, position - 0x8000];
			}
			else if (position > 0xFDFF && position < 0xFEA0)
			{
				if (LCDScreenOn) return oamAccessAllowed ? OAM[position - 0xFE00] : (byte)0xFF;
				else return OAM[position - 0xFE00];
			}
			else switch (position & 0xFF)
				{
					case IOPorts.LCDC:
						return LCDControl;
					case IOPorts.STAT:
						return LCDStatus;
					case IOPorts.SCX:
						return ScrollX;
					case IOPorts.SCY:
						return ScrollY;
					case IOPorts.LY:
						return LY;
					case IOPorts.LYC:
						return LYCompare;
					case IOPorts.BGP:
						return BackgroundPaletteData;
					case IOPorts.OBP0:
						return ObjectPalette0Data;
					case IOPorts.OBP1:
						return ObjectPalette1Data;
					case IOPorts.WX:
						return WindowX;
					case IOPorts.WY:
						return WindowY;
					case IOPorts.VBK:
						return (byte)VramBank;
					case IOPorts.BCPD:
					case IOPorts.BCPS:
					case IOPorts.OCPS:
					case IOPorts.OCPD:
					case IOPorts.DMA:
					case IOPorts.KEY1:
					case IOPorts.HDMA1:
					case IOPorts.HDMA2:
					case IOPorts.HDMA3:
					case IOPorts.HDMA4:
					case IOPorts.HDMA5:
					case IOPorts.RP:
					case IOPorts.SVBK:
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
					VRAM[VramBank, position - 0x8000] = data;
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
					case IOPorts.LCDC:
						LCDControl = data;
						CheckLCDCStatus();
						break;
					case IOPorts.STAT:
						LCDStatus = data;
						break;
					case IOPorts.SCX:
						ScrollX = data;
						break;
					case IOPorts.SCY:
						ScrollY = data;
						break;
					case IOPorts.LYC:
						LYCompare = data;
						break;
					case IOPorts.BGP:
						BackgroundPaletteData = data;
						UpdateGBBackgroundPalette();
						break;
					case IOPorts.OBP0:
						ObjectPalette0Data = data;
						UpdateObjectPalette0DMG();
						break;
					case IOPorts.OBP1:
						ObjectPalette1Data = data;
						UpdateObjectPalette1DMG();
						break;
					case IOPorts.WX:
						WindowX = data;
						break;
					case IOPorts.WY:
						WindowY = data;
						break;
					case IOPorts.VBK:
						//VramBank = data & 1;
						break;
					case IOPorts.BCPD:
					case IOPorts.BCPS:
					case IOPorts.OCPS:
					case IOPorts.OCPD:
					case IOPorts.DMA:
						DMATransferRequest = true;
						DMACounter = 200;
						break;
					case IOPorts.KEY1:
					case IOPorts.HDMA1:
					case IOPorts.HDMA2:
					case IOPorts.HDMA3:
					case IOPorts.HDMA4:
					case IOPorts.HDMA5:
					case IOPorts.RP:
					case IOPorts.SVBK:
						break;
				}
			}
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
					LCDMode = LCDMode.Mode2;
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
			int pix0 = VRAM[0, lineAddr + (lineOffset * 2)]  >> (7 - pixelNumFromLeft) & 1;
			int pix1 = (VRAM[0, lineAddr + (lineOffset * 2) + 1] >> (7 - pixelNumFromLeft) << 1) & 2;
			return pix0 + pix1;
		}

		private int DrawDMGScanline()
		{
			int SpritesDrawn = 0;
			if (LCDEnabled && LY < LCDHeight)
			{
				if (DMGBackgroundEnabled)
				{
					DrawDMGTiles();
				}
				if (SpritesEnabled)
				{
					SpritesDrawn = DrawDMGSprites();
				}
				LY++;
			}
			return SpritesDrawn;
		}

		private void DrawDMGTiles()
		{
			bool isScanlineIntersectingWindow = (IsWindowEnabled && WindowY <= LY);
			int[] lastTileLine = new int[8];
			byte BGY = (byte)(ScrollY + LY);
			byte BGX = 0;
			byte WinY = (byte)(LY - WindowY);
			byte WinX = 0;
			for (int LCD_X = 0; LCD_X < LCDWidth; LCD_X++)
			{
				//For drawing from window
				if (isScanlineIntersectingWindow && LCD_X >= (WindowX - 7))
				{
					WinX = (byte)(LCD_X - (WindowX - 7));
					byte TileIndexWin = VRAM[0, WindowTileMapStart + ((WinY >> 3) * TileMapXCount) + (WinX >> 3)];
					if (isComplementTileIndexingUsed) TileIndexWin += 0x80;
					int TilePosWin = BGWinTileDataStart + (TileIndexWin * 0x10);
					int TileOffYWin = WinY & 0x7;
					int TileOffXWin = WinX & 0x7;
					FetchTileLineFromTile(TilePosWin, TileOffYWin, lastTileLine, false, false);
					LCDMap[LY * LCDStride + LCD_X + 0] = GBColor.Colors[lastTileLine[TileOffXWin]][0];
					LCDMap[LY * LCDStride + LCD_X + 1] = GBColor.Colors[lastTileLine[TileOffXWin]][1];
					LCDMap[LY * LCDStride + LCD_X + 2] = GBColor.Colors[lastTileLine[TileOffXWin]][2];
				}
				//For drawing from map
				else
				{
					BGX = (byte)(ScrollX + LCD_X);
					byte TileIndexBG = VRAM[0, BGTileMapStart + ((BGY >> 3) * TileMapXCount) + (BGX >> 3)];
					if (isComplementTileIndexingUsed) TileIndexBG += 0x80;
					int TilePosBG = BGWinTileDataStart + (TileIndexBG * 0x10);
					int TileOffYBG = BGY & 0x7;
					int TileOffXBG = BGX & 0x7;
					FetchTileLineFromTile(TilePosBG, TileOffYBG, lastTileLine, false, false);
					LCDMap[LY * LCDStride + LCD_X + 0] = GBColor.Colors[lastTileLine[TileOffXBG]][0];
					LCDMap[LY * LCDStride + LCD_X + 1] = GBColor.Colors[lastTileLine[TileOffXBG]][1];
					LCDMap[LY * LCDStride + LCD_X + 2] = GBColor.Colors[lastTileLine[TileOffXBG]][2];
				}
			}
		}

		private void FetchTileLineFromTile(int position, int lineNum, int[] outArr, bool XFlip, bool YFlip)
		{
			int pos = position + (YFlip ? ((7 - lineNum) * 2) : (lineNum * 2));
			for (int i = 0; i < 8; i++)
			{
				int plane0 = VRAM[0, pos + 0] >> (XFlip ? i : (7 - i));
				int plane1 = VRAM[0, pos + 1] >> (XFlip ? i : (7 - i));
				outArr[i] = plane0 + (plane1 * 2);
			}
		}

		struct SpriteRef
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
			public int TileIndex;
			public int SpriteProperties;
			public static bool operator <(SpriteRef left, SpriteRef right)
			{
				return left.XOffset != right.XOffset ? left.XOffset < right.XOffset : left.OAMIndex < right.OAMIndex;
			}
			public static bool operator >(SpriteRef left, SpriteRef right)
			{
				return left.XOffset != right.XOffset ? left.XOffset > right.XOffset : left.OAMIndex > right.OAMIndex;
			}
		}

		public void ReconstructOAMTableDMG()
		{
			for (int i = 0; i < 40; i++)
			{
				SpriteTable[i] = new SpriteRef()
				{
					OAMIndex = i * 4,
					YOffset = OAM[i * 4],
					XOffset = OAM[(i * 4) + 1],
					TileIndex = OAM[(i * 4) + 2],
					SpriteProperties = OAM[(i * 4) + 3]
				};
				//If in CGB mode:
				//-Sprites are drawn by their OAM index, then their X position. Lower OAM takes priority.
				//If in DMG mode:
				//-Sprites are sorted by their X position. In the case that their Xs are equal,
				//-the one with the lower OAM position takes priority.
				int x = i;
				while (x > 0 && (SpriteTable[x] < SpriteTable[x - 1]))
				{
					SpriteRef temp = SpriteTable[x - 1];
					SpriteTable[x - 1] = SpriteTable[x];
					SpriteTable[x] = temp;
					x--;
				}
			}
		}

		private int DrawDMGSprites()
		{
			//Sprites always take their tiles from 8000-8FFF.
			if (SpritesEnabled)
			{
				int LCD_X = 0;
				int LineSpriteCount = 0;
				int[] lastTileLine = new int[8];
				//Run through each sprite in the table.
				for (int i = 0; i < SpriteTable.Length; i++)
				{
					SpriteRef r = SpriteTable[i];
					//If the sprite is on the line...
					if (LY >= r.YOffset - 16 && LY < r.YOffset)
					{
						//Increment the sprite count.
						LineSpriteCount++;
						//And if it's drawable to the screen (since it's on the LY, check if X pos is within screen)...
						if (r.XOffset > LCD_X && r.XOffset > 0 && r.XOffset < LCDWidth - 8)
						{
							int pixelLine = r.YOffset - LY;
							int tilePosition = r.TileIndex * 0x10;
							bool XFlip = (r.SpriteProperties & SpriteXFlip) != 0;
							bool YFlip = (r.SpriteProperties & SpriteYFlip) != 0;
							//If the tile is actually to be drawn...
							if (pixelLine < 8 || SpriteHeight == 16)
							{
								if (r.XOffset - 8 >= LCD_X) LCD_X = r.XOffset - 8;
								if (SpriteHeight == 16)
								{
									pixelLine -= 8;
									tilePosition++;
								}
								FetchTileLineFromTile(tilePosition, pixelLine, lastTileLine, XFlip, YFlip);
								for (int j = r.XOffset - LCD_X; j < r.XOffset; j++)
								{
									//Priority 0 : Sprites draw over BG, except in the case of color 0.
									//Priority 1 : Sprites aren't drawn over BG, except when color is white (?)
									if (((r.SpriteProperties & SpritePriority) == 0) ? lastTileLine[j] != 0 : IsPixelWhite(LY, LCD_X))
									{
										LCDMap[LY * LCDStride + LCD_X + 0] = GBColor.Colors[lastTileLine[j]][0];
										LCDMap[LY * LCDStride + LCD_X + 1] = GBColor.Colors[lastTileLine[j]][1];
										LCDMap[LY * LCDStride + LCD_X + 2] = GBColor.Colors[lastTileLine[j]][2];
									}
								}
							}
						}
					}
				}
				return LineSpriteCount;
			}
			else return 0;
		}

		private bool IsPixelWhite(int ly, int lx)
		{
			return LCDMap[ly * LCDStride + lx + 0] == 0x00
				&& LCDMap[ly * LCDStride + lx + 1] == 0x00
				&& LCDMap[ly * LCDStride + lx + 2] == 0x00;
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
					case LCDMode.Mode2://Mode 2: Searching OAM...no access to OAM, progresses to 3
						if (LineCounter >= Mode2Cycles)
						{
							LineCounter -= Mode2Cycles;
							//When switching to 3, write the current scanline.
							LCDMode = LCDMode.Mode3;
							DrawDMGScanline();
						}
						break;
					case LCDMode.Mode3://Mode 3: Searching OAM/VRAM...no access to OAM/VRAM or Palette Data, progresses to 0
						if (LineCounter >= Mode3Cycles)
						{
							//When switching to mode 0, the line is already drawn, so do nothing.
							LCDMode = LCDMode.Mode0;
							LineCounter -= Mode3Cycles;
							if (Mode0_HBlankInterruptEnabled || (LYCCoincidenceInterruptEnabled && (LYCoincidence != 0)))
							{
								interruptManager.RequestInterrupt(InterruptType.LCDC);
							}
						}
						break;
					case LCDMode.Mode0://Mode 0: HBlank...Access allowed, progresses to 1 (if at end of screen draw), or 2.
						if (LineCounter >= Mode0Cycles)
						{
							LineCounter -= Mode0Cycles;
							IncrementLY();
							if (LY >= LCDHeight)
							{
								LCDMode = LCDMode.Mode1;
								if (Mode1_VBlankInterruptEnabled) interruptManager.RequestInterrupt(InterruptType.LCDC);
								interruptManager.RequestInterrupt(InterruptType.VBlank);
								screen.CopyData(LCDMap);
							}
							else
							{
								LCDMode = LCDMode.Mode2;
							}
						}
						break;
					case LCDMode.Mode1://Mode 1: VBlank...access allowed, progresses to 2
						if (LineCounter >= LineDrawCycles)
						{
							LineCounter -= LineDrawCycles;
							IncrementLY();
							if (LY == 0)
							{
								CycleCounter -= LCDDrawCycles;
								LCDMode = LCDMode.Mode2;
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
			if (LY == LYCompare && LYCCoincidenceInterruptEnabled) interruptManager.RequestInterrupt(InterruptType.LCDC);
		}
	}
}
