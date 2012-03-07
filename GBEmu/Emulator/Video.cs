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

	public struct SpriteInfo
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
		public static bool operator <(SpriteInfo left, SpriteInfo right)
		{
			return left.XOffset != right.XOffset ? left.XOffset < right.XOffset : left.OAMIndex < right.OAMIndex;
		}
		public static bool operator >(SpriteInfo left, SpriteInfo right)
		{
			return left.XOffset != right.XOffset ? left.XOffset > right.XOffset : left.OAMIndex > right.OAMIndex;
		}
	}

	public enum LCDMode : byte { Mode0, Mode1, Mode2, Mode3 }

	class Video : TimedIODevice
	{
		#region Video constants
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

		#region Screen constants
		private const int LCDWidth = 160;
		private const int LCDHeight = 144;
		private const int LCDStride = LCDWidth;
		private const int LCDArraySize = LCDStride * LCDHeight;

		private const int TileWidth = 8;
		private const int TileHeight = 8;

		private const int TileMapXCount = 32;

		private const byte LYLimit = 154;
		#endregion

		private const byte STAT_MODEFLAG = 0x03;
		#endregion

		#region Video components
		private InterruptManager interruptManager;
		private IRenderable screen;
		#endregion

		#region LCD state and access permissions
		private LCDMode LCDState
		{
			get
			{
				return (LCDMode)(LCDStatus & STAT_MODEFLAG);
			}
			set
			{
				LCDStatus &= 0xF8;
				LCDStatus |= (byte)value;
			}
		}
		private bool OAMAccessAllowed
		{
			get
			{
				return (LCDState == LCDMode.Mode1) || (LCDState == LCDMode.Mode0);
			}
		}
		private bool VRAMAccessAllowed
		{
			get
			{
				return LCDState != LCDMode.Mode3;
			}
		}
		#endregion

		#region LCD control/status
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
		private byte LCDStatus;//FF41
		#region LCD Status options
		private bool LYCoincidenceInterruptEnabled
		{
			get
			{
				return (LCDStatus & STAT_LYC_LY_INTERRUPT_FLAG) != 0;
			}
		}//Bit 7
		private bool Mode2_OAMInterruptEnabled
		{
			get
			{
				return (LCDStatus & STAT_MODE2_OAM_INTERRUPT) != 0;
			}
		}//Bit 6
		private bool Mode1_VBlankInterruptEnabled
		{
			get
			{
				return (LCDStatus & STAT_MODE1_VBLANK_INTERRUPT) != 0;
			}
		}//Bit 5
		private bool Mode0_HBlankInterruptEnabled
		{
			get
			{
				return (LCDStatus & STAT_MODE0_HBLANK_INTERRUPT) != 0;
			}
		}//Bit 4
		private bool LYCoincidence
		{
			set
			{
				if (value)
				{
					LCDStatus |= STAT_COINCIDENCE_FLAG;
				}
				else
				{
					LCDStatus &= STAT_COINCIDENCE_FLAG_OFF;
				}
			}
			get
			{
				return (LCDStatus & STAT_COINCIDENCE_FLAG) != 0;
			}
		}//Bit 3
		#endregion
		#endregion

		#region Scroll
		private byte ScrollY;//FF42
		private byte ScrollX;//FF43
		#endregion

		#region LY
		private byte LY;//FF44
		private byte LYCompare;//FF45
		#endregion

		#region Background/object palettes
		private byte BackgroundPaletteData;//FF47
		private byte ObjectPalette0Data;//FF48
		private byte ObjectPalette1Data;//FF49
		private ARGBColor[] BGPalette_DMG;
		private ARGBColor[] OBJPalette0_DMG;
		private ARGBColor[] OBJPalette1_DMG;
		private ARGBColor[][] ObjectPalettes;
		#endregion

		#region Window
		private byte WindowY;//FF4A
		private byte WindowX;//FF4B
		#endregion

		public int ExecutedFrameCycles;
		private int TimeToNextModeChange;
		private int SpriteCountOnCurrentLine;

		#region STAT Constants
		#region Enable Constants (OR)
		private const byte STAT_LYC_LY_INTERRUPT_FLAG = 0x40;
		private const byte STAT_MODE2_OAM_INTERRUPT = 0x20;
		private const byte STAT_MODE1_VBLANK_INTERRUPT = 0x10;
		private const byte STAT_MODE0_HBLANK_INTERRUPT = 0x08;
		private const byte STAT_COINCIDENCE_FLAG = 0x04;
		#endregion

		#region Disable Constants (AND)
		private const byte STAT_COINCIDENCE_FLAG_OFF = 0xFB;
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

		public byte[] VRAM;//0x8000-0x9FFF
		
		public byte[] OAM;

		private bool LCDScreenOn;

		private SpriteInfo[] SpriteInfoTable;
		private static int SpriteInfoSize = 4;

		public Video(InterruptManager iM, IRenderable newScreen)
		{
			ExecutedFrameCycles = 0;
			screen = newScreen;
			interruptManager = iM;
			InitializeVideoMemory();
			InitializePalettes();
			InitializeLCD();
			LCDScreenOn = true;
			ShiftMode(LCDMode.Mode2);
		}

		#region Initialization
		private void InitializeLCD()
		{
			LCDMap = new int[LCDArraySize];
			LCDControl = 0x91;
			ScrollX = 0x00;
			ScrollY = 0x00;
			LYCompare = 0x00;
			WindowX = 0x00;
			WindowY = 0x00;
		}

		private void InitializeVideoMemory()
		{
			VRAM = new byte[0x2000];
			OAM = new byte[0xA0];
			SpriteInfoTable = new SpriteInfo[40];
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
			UpdateBackgroundPalette();
			UpdateObjectPalette0();
			UpdateObjectPalette1();
		}
		#endregion

		#region Reads
		public override byte Read(int position)
		{
			position &= 0xFFFF;
			if (position >= 0x8000 && position < 0xA000)
			{
				return VRAMRead(position);
			}
			else if (position >= 0xFE00 && position < 0xFEA0)
			{
				return OAMRead(position);
			}
			else if (position >= 0xFF40)
			{
				switch (position & 0xFF)
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
					default:
						return 0xFF;
				}
			}
			else return 0xFF;
		}

		private byte VRAMRead(int position)
		{
			if (position >= 0x8000 && position < 0xA000 && ((LCDScreenOn && VRAMAccessAllowed) || !LCDScreenOn))
			{
				return VRAM[position - 0x8000];
			}
			else return 0xFF;
		}

		private byte OAMRead(int position)
		{
			if (position >= 0xFE00 && position < 0xFEA0 & ((LCDScreenOn && OAMAccessAllowed) || !LCDScreenOn))
			{
				return OAM[position - 0xFE00];
			}
			else return 0xFF;
		}
		#endregion

		#region Writes
		public override void Write(int position, byte data)
		{
			position &= 0xFFFF;
			if (position >= 0x8000 && position < 0xA000)
			{
				VRAMWrite(position, data);
			}
			else if (position >= 0xFE00 && position < 0xFEA0)
			{
				OAMWrite(position, data);
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
						LCDStatus |= (byte)(data & 0x78);
						break;
					case IOPorts.SCX:
						ScrollX = data;
						break;
					case IOPorts.SCY:
						ScrollY = data;
						break;
					case IOPorts.LY:
						ResetLY();
						break;
					case IOPorts.LYC:
						LYCompare = data;
						break;
					case IOPorts.BGP:
						BackgroundPaletteData = data;
						UpdateBackgroundPalette();
						break;
					case IOPorts.OBP0:
						ObjectPalette0Data = data;
						UpdateObjectPalette0();
						break;
					case IOPorts.OBP1:
						ObjectPalette1Data = data;
						UpdateObjectPalette1();
						break;
					case IOPorts.WX:
						WindowX = data;
						break;
					case IOPorts.WY:
						WindowY = data;
						break;
					default: 
						break;
				}
			}
		}

		private void VRAMWrite(int position, byte data)
		{
			if (position >= 0x8000 && position < 0xA000)
			{
				if ((LCDScreenOn && VRAMAccessAllowed) || !LCDScreenOn)
				{
					VRAM[position - 0x8000] = data;
				}
			}
		}

		private void OAMWrite(int position, byte data)
		{
			if ((LCDScreenOn && OAMAccessAllowed) || !LCDScreenOn)
			{
				OAM[position - 0xFE00] = data;
			}
		}
		#endregion

		#region LCD general control
		private void TurnOffLCD()
		{
			if (LCDScreenOn)
			{
				LCDScreenOn = false;
				ResetLY();
			}
		}

		private void TurnOnLCD()
		{
			if (!LCDScreenOn)
			{
				LCDScreenOn = true;
				ResetLY();
			}
		}

		private void IncrementLY()
		{
			LY++;
			if (LY >= LYLimit) LY = 0;
			if (LY == LYCompare)
			{
				LYCoincidence = true;
				if (LYCoincidenceInterruptEnabled) interruptManager.RequestInterrupt(InterruptType.LCDC);
			}
		}

		private void ResetLY()
		{
			CycleCounter = 0;
			LY = 0;
			ShiftMode(LCDMode.Mode2);
		}

		private void ShiftMode(LCDMode newMode)
		{
			LCDState = newMode;
			switch (newMode)
			{
				case Emulator.LCDMode.Mode0://HBlank
					DrawScanline();
					TimeToNextModeChange = Mode0Cycles - (SpriteCountOnCurrentLine * 10);
					if (Mode0_HBlankInterruptEnabled)
					{
						interruptManager.RequestInterrupt(InterruptType.LCDC);
					}
					break;
				case Emulator.LCDMode.Mode1://VBlank
					TimeToNextModeChange = LineDrawCycles;
					if (LY == LCDHeight)
					{
						if (Mode1_VBlankInterruptEnabled)
						{
							interruptManager.RequestInterrupt(InterruptType.LCDC);
						}
						interruptManager.RequestInterrupt(InterruptType.VBlank);
						screen.CopyData(LCDMap);
					}
					break;
				case Emulator.LCDMode.Mode2://Searching OAM
					ReconstructOAMTable();
					SpriteCountOnCurrentLine = GetSpriteCountOnCurrentScanline();
					TimeToNextModeChange = Mode2Cycles;
					if (Mode2_OAMInterruptEnabled) interruptManager.RequestInterrupt(InterruptType.LCDC);
					break;
				case Emulator.LCDMode.Mode3://Searching OAM + VRAM
					TimeToNextModeChange = Mode3Cycles + (SpriteCountOnCurrentLine * 10);
					break;
			}
		}
		#endregion

		#region Palette updating
		public void UpdateBackgroundPalette()
		{
			for (int ColorNumber = 0; ColorNumber < 4; ColorNumber++)
			{
				int ShadeIndex = (BackgroundPaletteData >> (ColorNumber * 2)) & 0x03;
				BGPalette_DMG[ColorNumber] = DMGPredefColor.Colors[ShadeIndex];
			}
		}

		public void UpdateObjectPalette0()
		{
			for (int ColorNumber = 1; ColorNumber < 4; ColorNumber++)//Only copy for colors 1-3, because 0 is transparent
			{
				int ShadeIndex = (ObjectPalette0Data >> (ColorNumber * 2)) & 0x03; //This contains the shade for Color i
				OBJPalette0_DMG[ColorNumber] = DMGPredefColor.Colors[ShadeIndex];
			}
		}

		public void UpdateObjectPalette1()
		{
			for (int ColorNumber = 1; ColorNumber < 4; ColorNumber++)//Only copy for colors 1-3, because 0 is transparent
			{
				int ShadeIndex = (ObjectPalette1Data >> (ColorNumber * 2)) & 0x03; //This contains the shade for Color i
				OBJPalette0_DMG[ColorNumber] = DMGPredefColor.Colors[ShadeIndex];
			}
		}
		#endregion

		#region Scanline drawing
		private int DrawScanline()
		{
			int SpritesDrawn = 0;
			if (LY < LCDHeight)
			{
				if (LCDEnabled)
				{
					if (DMGBackgroundEnabled)
					{
						DrawTiles();
					}
					if (SpritesEnabled)
					{
						SpritesDrawn = DrawSprites();
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
				SetPixel((LY * LCDStride) + LCD_X, DMGPredefColor.White);
			}
		}

		private void DrawTiles()
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
					byte TileIndexWin = VRAM[WindowTileMapStart + ((WinY >> 3) * TileMapXCount) + (WinX >> 3)];
					if (isComplementTileIndexingUsed) TileIndexWin += 0x80;
					int TilePosWin = BGWinTileDataStart + (TileIndexWin * 0x10);
					int TileOffYWin = WinY & 0x7;
					int TileOffXWin = WinX & 0x7;
					FetchTileLineFromTile(TilePosWin, TileOffYWin, lastTileLine, false, false);
					SetPixel(LY * LCDStride + LCD_X, BGPalette_DMG[lastTileLine[TileOffXWin]]);
				}
				//For drawing from map
				else
				{
					BGX = (byte)(ScrollX + LCD_X);
					byte TileIndexBG = VRAM[BGTileMapStart + ((BGY >> 3) * TileMapXCount) + (BGX >> 3)];
					if (isComplementTileIndexingUsed) TileIndexBG += 0x80;
					int TilePosBG = BGWinTileDataStart + (TileIndexBG * 0x10);
					int TileOffYBG = BGY & 0x7;
					int TileOffXBG = BGX & 0x7;
					FetchTileLineFromTile(TilePosBG, TileOffYBG, lastTileLine, false, false);
					SetPixel(LY * LCDStride + LCD_X, BGPalette_DMG[lastTileLine[TileOffXBG]]);
				}
			}
		}

		private int DrawSprites()
		{
			if (SpritesEnabled)
			{
				int LCD_X = 0;
				int LineSpriteCount = 0;
				int[] lastTileLine = new int[TileWidth];
				for (int i = 0; i < SpriteInfoTable.Length; i++)
				{
					SpriteInfo r = SpriteInfoTable[i];
					if (LY >= r.YOffset - 16 && LY < r.YOffset)
					{
						LineSpriteCount++;
						if (r.XOffset > LCD_X && r.XOffset > 0 && r.XOffset < LCDWidth - TileWidth)
						{
							int pixelLine = r.YOffset - LY;
							int tilePosition = r.TileIndex * 0x10;
							bool XFlip = (r.SpriteProperties & SpriteXFlip) != 0;
							bool YFlip = (r.SpriteProperties & SpriteYFlip) != 0;
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
										SetPixel(LY * LCDStride + LCD_X, ObjectPalettes[r.OAMIndex][lastTileLine[j]]);
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

		private void FetchTileLineFromTile(int position, int lineNum, int[] outArr, bool XFlip, bool YFlip)
		{
			int pos = position + (YFlip ? ((7 - lineNum) * 2) : (lineNum * 2));
			for (int i = 0; i < TileWidth; i++)
			{
				int plane0 = VRAM[pos + 0] >> (XFlip ? i : (7 - i));
				int plane1 = VRAM[pos + 1] >> (XFlip ? i : (7 - i));
				outArr[i] = plane0 + (plane1 * 2);
			}
		}

		private int GetSpriteCountOnCurrentScanline()
		{
			int spriteCount = 0;
			for (int i = 0; i < SpriteInfoTable.Length; i++)
			{
				if (LY >= SpriteInfoTable[i].YOffset - 16 && LY < SpriteInfoTable[i].YOffset) spriteCount++;

			}
			return spriteCount;
		}

		public void ReconstructOAMTable()
		{
			for (int i = 0; i < SpriteInfoTable.Length; i++)
			{
				SpriteInfoTable[i] = new SpriteInfo()
				{
					OAMIndex = i * SpriteInfoSize,
					YOffset = OAM[(i * SpriteInfoSize) + 0],
					XOffset = OAM[(i * SpriteInfoSize) + 1],
					TileIndex = OAM[(i * SpriteInfoSize) + 2],
					SpriteProperties = OAM[(i * SpriteInfoSize) + 3]
				};
				//-Sprites are sorted by their X position. In the case that their Xs are equal,
				//-the one with the lower OAM position takes priority.
				int x = i;
				while (x > 0 && (SpriteInfoTable[x] < SpriteInfoTable[x - 1]))
				{
					SpriteInfo temp = SpriteInfoTable[x - 1];
					SpriteInfoTable[x - 1] = SpriteInfoTable[x];
					SpriteInfoTable[x] = temp;
					x--;
				}
			}
		}
		#endregion

		#region Pixel setting
		private void SetPixel(int offset, ARGBColor color)
		{
			LCDMap[offset] = color.ARGBVal;
		}

		private bool IsPixelWhite(int ly, int lx)
		{
			return LCDMap[ly * LCDStride + lx + 0] == DMGPredefColor.White.ARGBVal;
		}
		#endregion

		public override void UpdateCounter(int cycles)
		{
			if (LCDScreenOn)
			{
				CycleCounter += cycles;
				ExecutedFrameCycles += cycles;
				#region Changing modes (need to add special case for drawing scanline 0)
				//Modes go 2 -> 3 -> 0 -> 2 ->...0 -> 1 -> 2
				//LY increases happen after 0, or after each line of 1.
				if (CycleCounter >= TimeToNextModeChange)
				{
					CycleCounter -= TimeToNextModeChange;
					switch (LCDState)
					{
						case LCDMode.Mode2://Mode 2: Searching OAM...no access to OAM, progresses to 3
							ShiftMode(Emulator.LCDMode.Mode3);
							break;
						case LCDMode.Mode3://Mode 3: Searching OAM/VRAM...no access to OAM/VRAM or Palette Data, progresses to 0
							ShiftMode(Emulator.LCDMode.Mode0);
							break;
						case LCDMode.Mode0://Mode 0: HBlank...Access allowed, progresses to 1 (if at end of screen draw), or 2.
							IncrementLY();
							if (LY >= LCDHeight)
							{
								ShiftMode(Emulator.LCDMode.Mode1);
							}
							else
							{
								ShiftMode(Emulator.LCDMode.Mode2);
							}
							break;
						case LCDMode.Mode1://Mode 1: VBlank...access allowed, progresses to 2
							IncrementLY();
							if (LY != 0)
							{
								ShiftMode(LCDMode.Mode1);
							}
							else
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
	}
}
