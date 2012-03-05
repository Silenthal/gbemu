using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using GBEmu.Render;
using System.Runtime.InteropServices;

namespace GBEmu.Emulator
{
	[StructLayout(LayoutKind.Explicit)]
	public struct ARGBColor
	{
		[FieldOffset(0)]
		public int ARGBVal;
		[FieldOffset(0)]
		public byte Red;
		[FieldOffset(1)]
		public byte Green;
		[FieldOffset(2)]
		public byte Blue;
		[FieldOffset(3)]
		public byte Alpha;
	}

	public class DMGPredefColor
	{
		public static ARGBColor Transparent = new ARGBColor { Alpha = 0 };
		public static ARGBColor White = new ARGBColor() { Red = 0xFF, Green = 0xFF, Blue = 0xFF, Alpha = 0xFF };
		public static ARGBColor LightGrey = new ARGBColor() { Red = 0xAA, Green = 0xAA, Blue = 0xAA, Alpha = 0xFF };
		public static ARGBColor DarkGrey = new ARGBColor() { Red = 0x55, Green = 0x55, Blue = 0x55, Alpha = 0xFF };
		public static ARGBColor Black = new ARGBColor() { Red = 0x00, Green = 0x00, Blue = 0x00, Alpha = 0xFF };
		public static ARGBColor[] Colors = { White, LightGrey, DarkGrey, Black };
	}

	public enum LCDMode : byte { Mode0, Mode1, Mode2, Mode3 }

	class Video : TimedIODevice
	{
		private InterruptManager interruptManager;
		private IRenderable screen;
		public int ExecutedFrameCycles;
		public bool IsCGB { get; set; }
		#region Screen constants
		private const int LCDWidth = 160;
		private const int LCDHeight = 144;
		private const int LCDStride = LCDWidth;
		private const int LCDArraySize = LCDStride * LCDHeight;
		
		private const int TileWidth = 8;
		private const int TileHeight = 8;
		
		private const int TileMapXCount = 32;
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
				return (LCDControl & LCDC_SPRITE_SIZE) == 0 ? TileHeight : TileHeight * 2;
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
		private bool oamAccessAllowed;
		private bool vramAccessAllowed;

		private byte ScrollX;//FF43
		private byte ScrollY;//FF42
		private byte WindowX;//FF4B
		private byte WindowY;//FF4A
		private byte LYCompare;//FF45
		private byte LY;//FF44

		private byte BackgroundPaletteData;
		private byte ObjectPalette0Data;
		private byte ObjectPalette1Data;

		private ARGBColor[] BGPalette_DMG;
		private ARGBColor[] OBJPalette0_DMG;
		private ARGBColor[] OBJPalette1_DMG;
		private ARGBColor[][] ObjectPalettes;

		private int TimeToNextModeChange;
		private int SpriteCountOnCurrentLine;

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
		private const int Mode1Cycles = 4560;
		private const int Mode2Cycles = 80;
		private const int Mode3Cycles = 172;
		private const int Mode0Cycles = 204;
		private const int LYOnScreenCycles = 65664;
		private const int LCDDrawCycles = 70224;
		private const int LineDrawCycles = 456;
		#endregion

		private int[] LCDMap;

		public byte[,] VRAM;//0x8000-0x9FFF, x2 for GBC
		
		public byte[] OAM;

		private bool LCDScreenOn;

		private int DMACounter;

		public bool DMATransferRequest = false;

		private SpriteRef[] SpriteTable;

		public Video(InterruptManager iM, IRenderable newScreen)
		{
			ExecutedFrameCycles = 0;
			screen = newScreen;
			interruptManager = iM;
			VramBank = 0;
			
			InitializeLCD();
			InitializeVideoMemory();
			InitializePalettes();
			IsCGB = false;
			LCDScreenOn = true;
			DMACounter = 0;
		}

		private void InitializeLCD()
		{
			LCDMap = new int[LCDArraySize];
			LCDMode = LCDMode.Mode2;
			LCDControl = 0x91;
			ScrollX = 0x00;
			ScrollY = 0x00;
			LYCompare = 0x00;
			WindowX = 0x00;
			WindowY = 0x00;
		}

		private void InitializeVideoMemory()
		{
			VRAM = new byte[2, 0x2000];
			OAM = new byte[0xA0];
			SpriteTable = new SpriteRef[40];
		}

		private void InitializePalettes()
		{
			BackgroundPaletteData = 0xFC;
			ObjectPalette0Data = 0xFF;
			ObjectPalette1Data = 0xFF;
			BGPalette_DMG = new ARGBColor[4];
			OBJPalette0_DMG = new ARGBColor[4];
			OBJPalette0_DMG[0] = DMGPredefColor.Transparent;
			OBJPalette1_DMG = new ARGBColor[4];
			OBJPalette1_DMG[0] = DMGPredefColor.Transparent;
			ObjectPalettes = new ARGBColor[2][] { OBJPalette0_DMG, OBJPalette1_DMG };
			UpdateGBBackgroundPalette();
			UpdateObjectPalette0DMG();
			UpdateObjectPalette1DMG();
		}

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
						if (!LCDEnabled) TurnOffLCD();
						else TurnOnLCD();
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

		private void TurnOffLCD()
		{
			if (LCDScreenOn)
			{
				LCDScreenOn = false;
				CycleCounter = 0;
				LY = 0;
				LCDMode = LCDMode.Mode2;
			}
		}

		private void TurnOnLCD()
		{
			if (!LCDScreenOn)
			{
				LCDScreenOn = true;
				CycleCounter = 0;
				LY = 0;
				LCDMode = LCDMode.Mode2;
			}
		}

		public void UpdateGBBackgroundPalette()
		{
			for (int ColorNumber = 0; ColorNumber < 4; ColorNumber++)
			{
				int ShadeIndex = (BackgroundPaletteData >> (ColorNumber * 2)) & 0x03;
				BGPalette_DMG[ColorNumber] = DMGPredefColor.Colors[ShadeIndex];
			}
		}

		public void UpdateObjectPalette0DMG()
		{
			for (int ColorNumber = 1; ColorNumber < 4; ColorNumber++)//Only copy for colors 1-3, because 0 is transparent
			{
				int ShadeIndex = (ObjectPalette0Data >> (ColorNumber * 2)) & 0x03; //This contains the shade for Color i
				OBJPalette0_DMG[ColorNumber] = DMGPredefColor.Colors[ShadeIndex];
			}
		}

		public void UpdateObjectPalette1DMG()
		{
			for (int ColorNumber = 1; ColorNumber < 4; ColorNumber++)//Only copy for colors 1-3, because 0 is transparent
			{
				int ShadeIndex = (ObjectPalette1Data >> (ColorNumber * 2)) & 0x03; //This contains the shade for Color i
				OBJPalette0_DMG[ColorNumber] = DMGPredefColor.Colors[ShadeIndex];
			}
		}

		private int DrawDMGScanline()
		{
			int SpritesDrawn = 0;
			if (LY < LCDHeight)
			{
				if (LCDEnabled)
				{
					if (DMGBackgroundEnabled)
					{
						DrawDMGTiles();
					}
					if (SpritesEnabled)
					{
						SpritesDrawn = DrawDMGSprites();
					}
				}
				else
				{
					DrawBlankScanline();
				}
			}
			return SpritesDrawn;
		}

		private void DrawBlankScanline()
		{
			for (int LCD_X = 0; LCD_X < LCDWidth; LCD_X++)
			{
				LCDSetPixel((LY * LCDStride) + LCD_X, DMGPredefColor.White);
			}
		}

		private void DrawDMGTiles()
		{
			bool isScanlineIntersectingWindow = (IsWindowEnabled && WindowY <= LY);
			int[] lastTileLine = new int[TileWidth];
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
					LCDSetPixel(LY * LCDStride + LCD_X, BGPalette_DMG[lastTileLine[TileOffXWin]]);
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
					LCDSetPixel(LY * LCDStride + LCD_X, BGPalette_DMG[lastTileLine[TileOffXBG]]);
				}
			}
		}

		private void LCDSetPixel(int offset, ARGBColor color)
		{
			LCDMap[offset] = color.ARGBVal;
		}

		private void FetchTileLineFromTile(int position, int lineNum, int[] outArr, bool XFlip, bool YFlip)
		{
			int pos = position + (YFlip ? ((7 - lineNum) * 2) : (lineNum * 2));
			for (int i = 0; i < TileWidth; i++)
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

		public int GetSpriteCountOnCurrentScanline()
		{
			int spriteCount = 0;
			for (int i = 0; i < SpriteTable.Length; i++)
			{
				if (LY >= SpriteTable[i].YOffset - 16 && LY < SpriteTable[i].YOffset) spriteCount++;
				
			}
			return spriteCount;
		}

		private int DrawDMGSprites()
		{
			//Sprites always take their tiles from 8000-8FFF.
			if (SpritesEnabled)
			{
				int LCD_X = 0;
				int LineSpriteCount = 0;
				int[] lastTileLine = new int[TileWidth];
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
						if (r.XOffset > LCD_X && r.XOffset > 0 && r.XOffset < LCDWidth - TileWidth)
						{
							int pixelLine = r.YOffset - LY;
							int tilePosition = r.TileIndex * 0x10;
							bool XFlip = (r.SpriteProperties & SpriteXFlip) != 0;
							bool YFlip = (r.SpriteProperties & SpriteYFlip) != 0;
							//If the tile is actually to be drawn...
							if (pixelLine < TileWidth || SpriteHeight == 16)
							{
								if (r.XOffset - TileWidth >= LCD_X) LCD_X = r.XOffset - TileWidth;
								if (SpriteHeight == 16)
								{
									pixelLine -= TileWidth;
									tilePosition++;
								}
								FetchTileLineFromTile(tilePosition, pixelLine, lastTileLine, XFlip, YFlip);
								for (int j = r.XOffset - LCD_X; j < r.XOffset; j++)
								{
									//Priority 0 : Sprites draw over BG, except in the case of color 0.
									//Priority 1 : Sprites aren't drawn over BG, except when color is white (?)
									if (((r.SpriteProperties & SpritePriority) == 0) ? lastTileLine[j] != 0 : IsPixelWhite(LY, LCD_X))
									{
										LCDSetPixel(LY * LCDStride + LCD_X, ObjectPalettes[r.OAMIndex][lastTileLine[j]]);
									}
								}
							}
						}
					}
					if (LineSpriteCount >= 10) break;
				}
				return LineSpriteCount;
			}
			else return 0;
		}

		private bool IsPixelWhite(int ly, int lx)
		{
			return (LCDMap[ly * LCDStride + lx + 0] & 0xFFFFFF) == 0x00;
		}

		public override void UpdateCounter(int cycles)
		{
			if (LCDScreenOn)
			{
				CycleCounter += cycles;
				ExecutedFrameCycles += cycles;
				if (DMATransferRequest)
				{
					DMACounter -= cycles;
					if (DMACounter <= 0)
					{
						DMACounter = 0;
						DMATransferRequest = false;
					}
				}
				#region Changing modes (need to add special case for drawing scanline 0)
				//Modes go 2 -> 3 -> 0 -> 2 ->...0 -> 1 -> 2
				//LY increases happen after 0, or after each line of 1.
				if (CycleCounter >= TimeToNextModeChange)
				{
					CycleCounter -= TimeToNextModeChange;
					switch (LCDMode)
					{
						case LCDMode.Mode2://Mode 2: Searching OAM...no access to OAM, progresses to 3
							ShiftMode(Emulator.LCDMode.Mode3);
							break;
						case LCDMode.Mode3://Mode 3: Searching OAM/VRAM...no access to OAM/VRAM or Palette Data, progresses to 0
							ShiftMode(Emulator.LCDMode.Mode0);
							if (Mode0_HBlankInterruptEnabled)
							{
								interruptManager.RequestInterrupt(InterruptType.LCDC);
							}
							break;
						case LCDMode.Mode0://Mode 0: HBlank...Access allowed, progresses to 1 (if at end of screen draw), or 2.
							IncrementLY();
							if (LY >= LCDHeight)
							{
								ShiftMode(Emulator.LCDMode.Mode1);
								screen.CopyData(LCDMap);
								if (Mode1_VBlankInterruptEnabled)
								{
									interruptManager.RequestInterrupt(InterruptType.LCDC);
								}
								interruptManager.RequestInterrupt(InterruptType.VBlank);
							}
							else
							{
								ShiftMode(Emulator.LCDMode.Mode2);
							}
							
							break;
						case LCDMode.Mode1://Mode 1: VBlank...access allowed, progresses to 2
							IncrementLY();
							if (LY == 0)
							{
								ShiftMode(Emulator.LCDMode.Mode2);
								ExecutedFrameCycles = 0;
							}
							break;
					}
				}
				#endregion
			}
		}

		private void ShiftMode(LCDMode newMode)
		{
			LCDMode = newMode;
			switch (newMode)
			{
				case Emulator.LCDMode.Mode0:
					DrawDMGScanline();
					TimeToNextModeChange = Mode0Cycles - (SpriteCountOnCurrentLine * 10);
					oamAccessAllowed = true;
					vramAccessAllowed = true;
					break;
				case Emulator.LCDMode.Mode1:
					TimeToNextModeChange = LineDrawCycles;
					oamAccessAllowed = true;
					vramAccessAllowed = true;
					break;
				case Emulator.LCDMode.Mode2:
					ReconstructOAMTableDMG();
					SpriteCountOnCurrentLine = GetSpriteCountOnCurrentScanline();
					TimeToNextModeChange = Mode2Cycles;
					oamAccessAllowed = false;
					vramAccessAllowed = true;
					break;
				case Emulator.LCDMode.Mode3:
					TimeToNextModeChange = Mode3Cycles + (SpriteCountOnCurrentLine * 10);
					oamAccessAllowed = false;
					vramAccessAllowed = false;
					break;
			}
		}

		private void IncrementLY()
		{
			LY++;
			if (LY >= LYLimit) LY = 0;
			if (LY == LYCompare && LYCCoincidenceInterruptEnabled) interruptManager.RequestInterrupt(InterruptType.LCDC);
		}

		public void SpeedSwitch()
		{
			
		}
	}
}
