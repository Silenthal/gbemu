using GBEmu.Render;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace GBEmu.Emulator
{
	public class DMGPredefColor
	{
		public static XnaColor Transparent = new XnaColor { A = 0 };
		public static XnaColor White = XnaColor.White;
		public static XnaColor LightGrey = XnaColor.LightGray;
		public static XnaColor DarkGrey = XnaColor.DarkGray;
		public static XnaColor Black = XnaColor.Black;
		public static XnaColor[] Colors = { White, LightGrey, DarkGrey, Black };
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
		public bool XFlip
		{
			get
			{
				return (SpriteProperties & 0x20) != 0;
			}
		}
		public bool YFlip
		{
			get
			{
				return (SpriteProperties & 0x40) != 0;
			}
		}
		public int VRAMBank
		{
			get
			{
				return (SpriteProperties >> 3) & 1;
			}
		}
		public int CGBPaletteNumber
		{
			get
			{
				return SpriteProperties & 0x7;
			}
		}
		public int DMGObjectPaletteNum
		{
			get
			{
				return (SpriteProperties >> 4) & 1;
			}
		}
		public bool PriorityOverBG
		{
			get
			{
				return (SpriteProperties & 0x80) == 0;
			}
		}
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
		private const int LCDWidth = 160;
		private const int LCDHeight = 144;
		private const int LCDStride = LCDWidth;
		private const int LCDArraySize = LCDStride * LCDHeight;

		private const int TileMapStride = 32;

		private const byte LYLimit = 154;
		#endregion

		#region Video components
		private InterruptManager interruptManager;
		private IRenderable screen;
		#endregion

		#region LCD access permissions
		private bool OAMAccessAllowed
		{
			get
			{
				return (LCDState == LCDMode.Mode1) || (LCDState == LCDMode.Mode0) || !LCDEnabled;
			}
		}
		private bool VRAMAccessAllowed
		{
			get
			{
				return (LCDState != LCDMode.Mode3) || !LCDEnabled;
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
				return (LCDControl & 0x80) != 0;
			}
		}//Bit 7
		private int WindowTileMapStart
		{
			get
			{
				return (LCDControl & 40) == 0 ? 0x1800 : 0x1C00;
			}
		}//Bit 6
		private bool IsWindowEnabled
		{
			get
			{
				return (LCDControl & 0x20) != 0;
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
				return (LCDControl & 0x08) == 0 ? 0x1800 : 0x1C00;
			}
		}//Bit 3
		private bool Sprite8By16Mode
		{
			get
			{
				return (LCDControl & 0x04) != 0;
			}

		}//Bit 2
		private bool SpritesEnabled
		{
			get
			{
				return (LCDControl & 0x02) != 0;
			}
		}//Bit 1
		private bool DMGBackgroundEnabled
		{
			get
			{
				return (LCDControl & 0x01) != 0;
			}
		}//Bit 0
		private bool isComplementTileIndexingUsed
		{
			get
			{
				return (LCDControl & 0x10) == 0;
			}
		}
		#endregion
		private byte LCDStatus;//FF41
		#region LCD Status options
		private bool LYCoincidenceInterruptEnabled
		{
			get
			{
				return (LCDStatus & 0x80) != 0;
			}
		}//Bit 7
		private bool Mode2_OAMInterruptEnabled
		{
			get
			{
				return (LCDStatus & 0x40) != 0;
			}
		}//Bit 6
		private bool Mode1_VBlankInterruptEnabled
		{
			get
			{
				return (LCDStatus & 0x20) != 0;
			}
		}//Bit 5
		private bool Mode0_HBlankInterruptEnabled
		{
			get
			{
				return (LCDStatus & 0x10) != 0;
			}
		}//Bit 4
		private bool LYCoincidence
		{
			set
			{
				if (value)
				{
					LCDStatus |= 0x08;
				}
				else
				{
					LCDStatus &= 0xF7;
				}
			}
			get
			{
				return (LCDStatus & 0x08) != 0;
			}
		}//Bit 3
		private LCDMode LCDState
		{
			get
			{
				return (LCDMode)(LCDStatus & 0x03);
			}
			set
			{
				LCDStatus &= 0xF8;
				LCDStatus |= (byte)value;
			}
		}//Bit 2-0
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
		private XnaColor[] BGPalette_DMG;
		private XnaColor[] OBJPalette0_DMG;
		private XnaColor[] OBJPalette1_DMG;
		private XnaColor[][] ObjectPalettes;
		#endregion

		#region Window
		private byte WindowY;//FF4A
		private byte WindowX;//FF4B
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

		private XnaColor[] LCDMap;

		public byte[] VRAM;//0x8000-0x9FFF
		
		public byte[] OAM;//0xFE00-0xFE9F

		public int ExecutedFrameCycles;

		private int TimeToNextModeChange;
		
		private int SpriteCountOnCurrentLine;

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
			ShiftMode(LCDMode.Mode2);
		}

		#region Initialization
		private void InitializeLCD()
		{
			LCDMap = new XnaColor[LCDArraySize];
			for (int i = 0; i < LCDMap.Length; i++)
			{
				LCDMap[i] = DMGPredefColor.White;
			}
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
			BGPalette_DMG = new XnaColor[4];
			OBJPalette0_DMG = new XnaColor[4];
			OBJPalette0_DMG[0] = DMGPredefColor.Transparent;
			OBJPalette1_DMG = new XnaColor[4];
			OBJPalette1_DMG[0] = DMGPredefColor.Transparent;
			ObjectPalettes = new XnaColor[2][] { OBJPalette0_DMG, OBJPalette1_DMG };
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
			if (position >= 0x8000 && position < 0xA000 && VRAMAccessAllowed)
			{
				return VRAM[position - 0x8000];
			}
			else return 0xFF;
		}

		private byte OAMRead(int position)
		{
			if (position >= 0xFE00 && position < 0xFEA0 & OAMAccessAllowed)
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
						bool LCDOnOff = LCDEnabled;
						LCDControl = data;
						if (!LCDOnOff && LCDEnabled) ResetLY();
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
				if (VRAMAccessAllowed)
				{
					VRAM[position - 0x8000] = data;
				}
			}
		}

		private void OAMWrite(int position, byte data)
		{
			if (OAMAccessAllowed)
			{
				OAM[position - 0xFE00] = data;
			}
		}
		#endregion

		#region LY/mode management
		private void IncrementLY()
		{
			LY++;
			if (LY >= LYLimit)
			{
				LY = 0;
			}
			if (LY == LYCompare)
			{
				LYCoincidence = true;
				if (LYCoincidenceInterruptEnabled) interruptManager.RequestInterrupt(InterruptType.LCDC);
			}
		}

		private void ResetLY()
		{
			LY = 0;
			if (LY == LYCompare)
			{
				LYCoincidence = true;
				if (LYCoincidenceInterruptEnabled) interruptManager.RequestInterrupt(InterruptType.LCDC);
			}
			CycleCounter = 0;
			ExecutedFrameCycles = 0;
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
		/// <summary>
		/// Updates the current DMG background palette.
		/// </summary>
		public void UpdateBackgroundPalette()
		{
			for (int ColorNumber = 0; ColorNumber < 4; ColorNumber++)
			{
				int ShadeIndex = (BackgroundPaletteData >> (ColorNumber * 2)) & 0x03;
				BGPalette_DMG[ColorNumber] = DMGPredefColor.Colors[ShadeIndex];
			}
		}

		/// <summary>
		/// Updates the current DMG OBP0 palette.
		/// </summary>
		public void UpdateObjectPalette0()
		{
			for (int ColorNumber = 1; ColorNumber < 4; ColorNumber++)//Only copy for colors 1-3, because 0 is transparent
			{
				int ShadeIndex = (ObjectPalette0Data >> (ColorNumber * 2)) & 0x03; //This contains the shade for Color i
				OBJPalette0_DMG[ColorNumber] = DMGPredefColor.Colors[ShadeIndex];
			}
		}


		/// <summary>
		/// Updates the current DMG OBP1 palette.
		/// </summary>
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
		/// <summary>
		/// Draws a scanline on the screen.
		/// </summary>
		private void DrawScanline()
		{
			if (LY < LCDHeight)
			{
				if (LCDEnabled)
				{
					DrawTiles();
					DrawSprites();
				}
				else
				{
					if (LY == 72) DrawBlackScanline();
					else DrawWhiteScanline();
				}
			}
		}

		/// <summary>
		/// Draws a white scanline on the LCD.
		/// </summary>
		private void DrawWhiteScanline()
		{
			for (int LCD_X = 0; LCD_X < LCDWidth; LCD_X++)
			{
				SetPixel(LCD_X, LY, DMGPredefColor.White);
			}
		}

		/// <summary>
		/// Draws a black scanline on the LCD.
		/// </summary>
		private void DrawBlackScanline()
		{
			for (int LCD_X = 0; LCD_X < LCDWidth; LCD_X++)
			{
				SetPixel(LCD_X, LY, DMGPredefColor.Black);
			}
		}

		/// <summary>
		/// Draws the background/window tiles on the current scanline.
		/// </summary>
		private void DrawTiles()
		{
			if (!(DMGBackgroundEnabled || IsWindowEnabled)) return;
			bool isScanlineIntersectingWindow = (IsWindowEnabled && WindowY <= LY);
			byte BGY = (byte)(ScrollY + LY);
			byte BGX = 0;
			byte WinY = (byte)(LY - WindowY);
			byte WinX = 0;
			for (int LCD_X = 0; LCD_X < LCDWidth; LCD_X++)
			{
				int TileMapX = 0;
				int TileMapY = 0;
				int TileMapStart = 0;
				if (isScanlineIntersectingWindow && LCD_X >= (WindowX - 7))
				{
					WinX = (byte)(LCD_X - (WindowX - 7));
					TileMapStart = WindowTileMapStart;
					TileMapX = WinX;
					TileMapY = WinY;
				}
				else
				{
					BGX = (byte)(ScrollX + LCD_X);
					TileMapStart = BGTileMapStart;
					TileMapX = BGX;
					TileMapY = BGY;
				}
				byte TileIndex = VRAM[TileMapStart + ((TileMapY >> 3) * 32) + (TileMapX >> 3)];
				if (isComplementTileIndexingUsed) TileIndex += 0x80;
				int TileOffset = BGWinTileDataStart + (TileIndex * 0x10);
				int TilePixelColor = GetPixelPaletteNumberFromTile(TileOffset, TileMapX & 0x7, TileMapY & 0x7, false, false);
				SetPixel(LCD_X, LY, BGPalette_DMG[TilePixelColor]);
			}
		}

		/// <summary>
		/// Draws the sprites on the current scanline.
		/// </summary>
		private void DrawSprites()
		{
			if (!SpritesEnabled) return;
			int LineSpriteCount = 0;
			for (int i = 0; i < SpriteInfoTable.Length; i++)
			{
				SpriteInfo r = SpriteInfoTable[i];
				if (LY >= r.YOffset - 16 && LY < r.YOffset) //If the sprite intersects LY...
				{
					LineSpriteCount++;//Increment the sprite count for the line.
					int SpriteTileY = LY + (r.YOffset - 16);
					if (!Sprite8By16Mode && SpriteTileY >= 8) continue;
					for (int LCD_X = r.XOffset - 8; LCD_X < r.XOffset; LCD_X++)
					{
						if (LCD_X < 0 || LCD_X > LCDWidth) continue;
						int SpriteTileX = LCD_X - (r.XOffset - 8);
						int SpriteTileOff = r.TileIndex * 0x10;
						if (Sprite8By16Mode && SpriteTileY >= 8)//If the sprite is drawing its second tile in 8x16 mode
						{
							//Use the second tile offset instead, which is right after the first.
							SpriteTileY -= 8;
							SpriteTileOff++;
						}
						int SpritePixelNum = GetPixelPaletteNumberFromTile(SpriteTileOff, SpriteTileX, SpriteTileY, r.XFlip, r.YFlip);
						//PriorityOverBG : Sprites are draw over BG, except in the case of color 0.
						//!PriorityOverBG : Sprites aren't drawn over BG, except when BG color is white (?)
						if (r.PriorityOverBG ? SpritePixelNum != 0 : GetPixel(LCD_X, LY) == DMGPredefColor.White)
						{
							SetPixel(LCD_X, LY, ObjectPalettes[r.DMGObjectPaletteNum][SpritePixelNum]);
						}
					}
				}
				if (LineSpriteCount >= 10) break;
			}
		}

		/// <summary>
		/// Fetches the palette number of the 'color' at the specified position inside a tile.
		/// </summary>
		/// <param name="tileOffset">The position of the tile in VRAM.</param>
		/// <param name="xPos">The X position of the pixel in the tile.</param>
		/// <param name="yPos">The Y position of the pixel in the tile.</param>
		/// <param name="xFlip">Whether the tile is flipped on the X axis.</param>
		/// <param name="yFlip">Whether the tile is flipped on the Y axis.</param>
		/// <returns>The palette number of the pixel.</returns>
		private int GetPixelPaletteNumberFromTile(int tileOffset, int xPos, int yPos, bool xFlip, bool yFlip)
		{
			int tileXpos = xFlip ? (7 - xPos) : xPos;
			int tileYpos = tileOffset + (yFlip ? ((7 - yPos) * 2) : (yPos * 2));
			return (((VRAM[tileYpos + 1] >> tileXpos) & 1) << 1) + ((VRAM[tileYpos] >> tileXpos) & 1);
		}

		/// <summary>
		/// Gets the number of sprites on the current line.
		/// </summary>
		/// <returns>The number of sprites on the line. If there are more than 10, 10 is returned.</returns>
		private int GetSpriteCountOnCurrentScanline()
		{
			int spriteCount = 0;
			for (int i = 0; i < SpriteInfoTable.Length; i++)
			{
				if (LY >= SpriteInfoTable[i].YOffset - 16 && LY < SpriteInfoTable[i].YOffset) spriteCount++;

			}
			if (spriteCount > 10) spriteCount = 10;
			return spriteCount;
		}

		/// <summary>
		/// Reconstructs the sprite info table from OAM.
		/// </summary>
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
		/// <summary>
		/// Sets a pixel on the main screen.
		/// </summary>
		/// <param name="x">The X position of the pixel.</param>
		/// <param name="y">The Y position of the pixel.</param>
		/// <param name="color">The color of the pixel.</param>
		private void SetPixel(int x, int y, XnaColor color)
		{
			LCDMap[(x * LCDStride) + y] = color;
		}

		/// <summary>
		/// Gets the pixel at the specified location in the LCD.
		/// </summary>
		/// <param name="x">The X position of the pixel.</param>
		/// <param name="y">THe Y position of the pixel.</param>
		/// <returns>The pixel at the location.</returns>
		private XnaColor GetPixel(int x, int y)
		{
			return LCDMap[(x * LCDStride) + y];
		}
		#endregion

		public override void UpdateCounter(int cycles)
		{
			CycleCounter += cycles;
			ExecutedFrameCycles += cycles;
			if (ExecutedFrameCycles >= LCDDrawCycles)
			{
				BlitScreen();
				ExecutedFrameCycles = 0;
			}
			#region Changing modes (need to add special case for drawing scanline 0)
			//Modes go 2 -> 3 -> 0 -> 2 ->...0 -> 1 -> ... -> 1 -> 2
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

		public void BlitScreen()
		{
			screen.CopyData(LCDMap);
		}
	}
}
