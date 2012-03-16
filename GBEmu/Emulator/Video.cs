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

	public class LYCounter : TimedIODevice
	{
		//When the LCD is turned on, reset LY.
		private static int LineCycles = 456;
		public byte LY { get; private set; }
		public byte LYC { get; set; }
		public int TimeToNextLine
		{
			get
			{
				return 456 - CycleCounter;
			}
		}
		public int TimeToNextFrame
		{
			get
			{
				return 70224 - (LY * 456) + CycleCounter;
			}
		}

		public delegate void OnLineChangedEventHandler();

		public event OnLineChangedEventHandler LineChanged;

		protected virtual void OnLineChanged()
		{
			if (LineChanged != null)
				LineChanged();
		}

		public override void UpdateCounter(int cycles)
		{
			CycleCounter += cycles;
			if (CycleCounter >= LineCycles)
			{
				CycleCounter -= LineCycles;
				IncrementLY();
			}
		}

		private void IncrementLY()
		{
			LY++;
			if (LY >= 154)
			{
				LY = 0;
			}
			OnLineChanged();
		}

		public void ResetLY()
		{
			LY = 0;
			CycleCounter = 0;
			OnLineChanged();
		}

		public override byte Read(int position)
		{
			if (position == IOPorts.LY)
			{
				return LY;
			}
			else return 0xFF;
		}

		public override void Write(int position, byte data)
		{

		}
	}

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
				return ((LCDMode & 0x2) == 0) || !LCDEnabled;
			}
		}
		private bool VRAMAccessAllowed
		{
			get
			{
				return (LCDMode != 3) || !LCDEnabled;
			}
		}
		#endregion

		#region LCD control/status
		/// <summary>
		/// [FF40]Controls the LCD, including window/background/sprite display.
		/// </summary>
		/// <remarks>
		/// Bit 7: LCD Enabled				(0 = off			1 = on)
		/// Bit 6: Window Tile Map			(0 = 0x9800-0x9BFF	1 = 0x9C00-0x9FFF)
		/// Bit 5: Window Enabled			(0 = off			1 = on)
		/// Bit 4: BG/Window Tile Map Used	(0 = 0x8800-0x97FF	1 = 0x8000-0x8FFF)
		/// Bit 3: BG Tile Map				(0 = 0x9800-0x9BFF	1 = 0x9C00-0x9FFF)
		/// Bit 2: Sprite Size				(0 = 8x8			1 = 8x16)
		/// Bit 1: Sprite Display Enabled	(0 = off			1 = on)
		/// Bit 0: BG Display (DMG)			(0 = off			1 = on)
		/// </remarks>
		private byte LCDControl;
		#region LCD Control Options
		/// <summary>
		/// Indicates whether the LCD is on or off. Set to false to turn off the LCD, and true to turn it on.
		/// </summary>
		private bool LCDEnabled
		{
			get
			{
				return (LCDControl & 0x80) != 0;
			}
		}//Bit 7
		/// <summary>
		/// Contains the start of the window tile map in VRAM.
		/// </summary>
		private int WindowTileMapStart
		{
			get
			{
				return (LCDControl & 0x40) == 0 ? 0x1800 : 0x1C00;
			}
		}//Bit 6
		/// <summary>
		/// Indicates whether the window is drawn. Set to false to disable window drawing.
		/// </summary>
		private bool WindowEnabled
		{
			get
			{
				return (LCDControl & 0x20) != 0;
			}
		}//Bit 5
		/// <summary>
		/// Contains the start of the tile data table used by the BG and window, as an offset in VRAM.
		/// </summary>
		private int BGWinTileDataStart
		{
			get
			{
				return isSignedTileIndex ? 0x800 : 0;
			}
		}//Bit 4
		/// <summary>
		/// Contains the start of the BG tile map in VRAM.
		/// </summary>
		private int BGTileMapStart
		{
			get
			{
				return (LCDControl & 0x08) == 0 ? 0x1800 : 0x1C00;
			}
		}//Bit 3
		/// <summary>
		/// Indicates whether 8x16 sprites are being used.
		/// </summary>
		private bool Sprite8By16Mode
		{
			get
			{
				return (LCDControl & 0x04) != 0;
			}

		}//Bit 2
		/// <summary>
		/// Indicates whether sprites are drawn. Set to false to disable sprite drawing.
		/// </summary>
		private bool SpritesEnabled
		{
			get
			{
				return (LCDControl & 0x02) != 0;
			}
		}//Bit 1
		/// <summary>
		/// Indicates whether the background is drawn (for DMG). Set to false to disable DMG background drawing.
		/// </summary>
		private bool DMGBackgroundEnabled
		{
			get
			{
				return (LCDControl & 0x01) != 0;
			}
		}
		/// <summary>
		/// Indicates whether the tile index used in the Map is interpreted as a signed index.
		/// </summary>
		private bool isSignedTileIndex
		{
			get
			{
				return (LCDControl & 0x10) == 0;
			}
		}
		#endregion
		/// <summary>
		/// [FF41]Controls the interrupts associated with the LCD, as well as indicates the status of the LCD drawing.
		/// </summary>
		/// <remarks>
		/// Bit 6: LY Coincidence Interrupt Enabled	(0 = off			1 = on)
		/// Bit 5: Mode 2 OAM Interrupt Enabled		(0 = off			1 = on)
		/// Bit 4: Mode 1 V-Blank Interrupt Enabled	(0 = off			1 = on)
		/// Bit 3: Mode 0 H-Blank Interrupt Enabled	(0 = off			1 = on)
		/// Bit 2: LY Coincidence Flag				(0 = (LY != LYC)	1 = (LY == LYC))
		/// Bit 1-0: Mode Flag
		///		-00: Mode 0: H-Blank				VRAM/OAM accessible.
		///		-01: Mode 1: V-Blank				VRAM/OAM accessible.
		///		-10: Mode 2: Reading from OAM		VRAM accessible, OAM inaccessible.
		///		-11: Mode 3: Reading from OAM/VRAM	VRAM/OAM inaccessible.
		/// </remarks>
		private byte LCDStatus;
		#region LCD Status 
		/// <summary>
		/// Indicates whether the LY=LYC coincidence interrupt is used.
		/// </summary>
		private bool LYCoincidenceInterruptEnabled
		{
			get
			{
				return (LCDStatus & 0x40) != 0;
			}
			set
			{
				if (value) LCDStatus |= 0x40;
				else LCDStatus &= 0xBF;
			}
		}//Bit 6
		/// <summary>
		/// Controls whether an interrupt is triggered when the LCD starts to search the OAM table.
		/// </summary>
		private bool Mode2_OAMInterruptEnabled
		{
			get
			{
				return (LCDStatus & 0x20) != 0;
			}
			set
			{
				if (value) LCDStatus |= 0x20;
				else LCDStatus &= 0xDF;
			}
		}//Bit 5
		/// <summary>
		/// Controls whether an interrupt is generated when the display enters V-Blank.
		/// </summary>
		private bool Mode1_VBlankInterruptEnabled
		{
			get
			{
				return (LCDStatus & 0x10) != 0;
			}
			set
			{
				if (value) LCDStatus |= 0x10;
				else LCDStatus &= 0xEF;
			}
		}//Bit 4
		/// <summary>
		/// Controls whether an interrupt is generated when the display enters H-Blank.
		/// </summary>
		private bool Mode0_HBlankInterruptEnabled
		{
			get
			{
				return (LCDStatus & 0x08) != 0;
			}
			set
			{
				if (value) LCDStatus |= 0x08;
				else LCDStatus &= 0xF7;
			}
		}//Bit 3
		/// <summary>
		/// Indicates whether LY is equal to LYC.
		/// </summary>
		private bool LYCoincidence
		{
			get
			{
				return (LCDStatus & 0x04) != 0;
			}
			set
			{
				if (value) LCDStatus |= 0x04;
				else LCDStatus &= 0xFB;
			}
		}//Bit 2
		/// <summary>
		/// Indicates the mode of the LCD.
		/// </summary>
		private int LCDMode
		{
			get
			{
				return LCDStatus & 0x03;
			}
			set
			{
				LCDStatus &= 0xFC;
				LCDStatus |= (byte)(value & 0x3);
			}
		}//Bit 1-0
		#endregion
		#endregion

		#region Scroll
		/// <summary>
		/// [FF42]Controls the Y position of the top left of the LCD in relation to the BG Map.
		/// </summary>
		private byte ScrollY;
		/// <summary>
		/// [FF43]Controls the X position of the top left of the LCD in relation to the BG Map.
		/// </summary>
		private byte ScrollX;
		#endregion

		#region LY
		private LYCounter lyCounter;
		/// <summary>
		/// [FF44]Contains the current scanline. Ranges from 0 to 153.
		/// </summary>
		private byte LY { get { return lyCounter.LY; } }
		/// <summary>
		/// [FF45]Holds a value that is compared with LY. When the two are equal, a flag and/or an interrupt can be triggered.
		/// </summary>
		private byte LYCompare;
		#endregion

		#region Background/object palettes
		/// <summary>
		/// [FF47]Contains the color numbers for the palette used by the BG Map.
		/// </summary>
		private byte BackgroundPaletteData;
		/// <summary>
		/// [FF48]Contains the color numbers for the sprite palette OBJ0.
		/// </summary>
		private byte ObjectPalette0Data;
		/// <summary>
		/// [FF49]Contains the color numbers for the sprite palette OBJ1.
		/// </summary>
		private byte ObjectPalette1Data;
		private XnaColor[] BGPalette_DMG;
		private XnaColor[] OBJPalette0_DMG;
		private XnaColor[] OBJPalette1_DMG;
		private XnaColor[][] ObjectPalettes;
		#endregion

		#region Window
		/// <summary>
		/// [FF4A]Contains the Y position of the top left corner of the Window in relation to the LCD.
		/// </summary>
		private byte WindowY;
		/// <summary>
		/// [FF4B]Contains the X position (minus 7) of the top left corner of the window in relation to the LCD.
		/// </summary>
		/// <example>
		/// To set the corner of window to the top right corner of the LCD, set WY to 0, and WX to 7.
		/// </example>
		private byte WindowX;
		#endregion

		#region Cycle Constants
		private const int Mode1Cycles = 4560;
		private const int Mode2Cycles = 80;
		private const int Mode3Cycles = 172;
		private const int Mode0Cycles = 204;
		private const int LYOnScreenCycles = 65664;
		private const int LCDDrawCycles = 70224;
		#endregion

		/// <summary>
		/// Represents the LCD that is being drawn to.
		/// </summary>
		private XnaColor[] LCDMap;

		/// <summary>
		/// [8000-9FFF]Video RAM.
		/// </summary>
		public byte[] VRAM;
		
		/// <summary>
		/// [FE00-FE9F]Object Attribute Memory.
		/// </summary>
		/// <remarks>
		/// OAM contains entries for 40 sprites. Each entry consists of 4 bytes:
		/// Byte 0: Y-Position
		/// Byte 1: X-Position
		/// Byte 2: Tile/Pattern Number
		/// Byte 3: Sprite Attributes
		/// -Bit 7: OBJ priority
		/// -Bit 6: Y Flip
		/// -Bit 5: X Flip
		/// -Bit 4: Palette Number (DMG)
		/// </remarks>
		public byte[] OAM;//0xFE00-0xFE9F

		public int ExecutedFrameCycles;

		private int TimeToNextModeChange;

		private SpriteInfo[] SpriteInfoTable;
		
		private static int SpriteInfoSize = 4;

		public Video(InterruptManager iM, IRenderable newScreen)
		{
			screen = newScreen;
			interruptManager = iM;
			lyCounter = new LYCounter();
			lyCounter.LineChanged += new LYCounter.OnLineChangedEventHandler(LYCoincidenceCheck);
			ResetLCD();
			InitializeVideoMemory();
			InitializePalettes();
			InitializeLCD();
			ChangeLCDMode(2);
		}

		#region Initialization
		private void InitializeLCD()
		{
			LCDMap = new XnaColor[LCDArraySize];
			for (int i = 0; i < LCDMap.Length; i++)
			{
				LCDMap[i] = DMGPredefColor.Black;
			}
			LCDControl = 0x91;
			LCDStatus = 0x80;
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
						if (LCDControl != data)
						{
							bool lcdChanged = ((LCDControl ^ data) & 0x80) != 0;
							if (lcdChanged)
							{
								bool lyc = LYCoincidence;
								ResetLCD();
								LCDMode = 00;
								if ((data & 0x80) != 0)//If LCD was turned on
								{
									LYCoincidence = false;
									ChangeLCDMode(2);
								}
								else
								{
									LYCoincidence = lyc;
								}
							}
							LCDControl = data;
						}
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
		private void LYCoincidenceCheck()
		{
			if (LCDEnabled && LY == LYCompare)
			{
				LYCoincidence = true;
				if (LYCoincidenceInterruptEnabled)
				{
					if ((!Mode2_OAMInterruptEnabled && LY < LCDHeight) || (!Mode1_VBlankInterruptEnabled && LY >= LCDHeight))
					{
						interruptManager.RequestInterrupt(InterruptType.LCDC);
					}
				}
			}
			else LYCoincidence = false;
		}

		private void ResetLCD()
		{
			lyCounter.ResetLY();
			CycleCounter = 0;
			ExecutedFrameCycles = 0;
		}

		/// <summary>
		/// Changes the mode of the LCD screen.
		/// </summary>
		/// <remarks>
		/// Mode 0: H-Blank. VRAM and OAM are accessible.
		/// Mode 1: V-Blank (or display disable). VRAM and OAM are accessible.
		/// Mode 2: OAM is inaccessible. VRAM is accessible.
		/// Mode 3: OAM and VRAM are inaccessible.
		/// </remarks>
		/// <param name="newMode">The mode to change to.</param>
		private void ChangeLCDMode(int newMode)
		{
			LCDMode = newMode;
			switch (newMode)
			{
				case 0://HBlank
					TimeToNextModeChange = Mode0Cycles - (GetSpriteCountOnCurrentScanline() * 10);
					if (LCDEnabled && Mode0_HBlankInterruptEnabled)
					{
						interruptManager.RequestInterrupt(InterruptType.LCDC);
					}
					break;
				case 1://VBlank
					TimeToNextModeChange = Mode1Cycles;
					if (LCDEnabled)
					{
						if (Mode1_VBlankInterruptEnabled)
						{
							interruptManager.RequestInterrupt(InterruptType.LCDC);
						}
						interruptManager.RequestInterrupt(InterruptType.VBlank);
					}
					break;
				case 2://Searching OAM
					ReconstructOAMTable();
					TimeToNextModeChange = Mode2Cycles;
					if (LCDEnabled && Mode2_OAMInterruptEnabled) interruptManager.RequestInterrupt(InterruptType.LCDC);
					break;
				case 3://Searching OAM + VRAM
					DrawScanline();
					TimeToNextModeChange = Mode3Cycles + (GetSpriteCountOnCurrentScanline() * 10);
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
					if (DMGBackgroundEnabled) DrawBackground();
					if (WindowEnabled) DrawWindow();
					if (SpritesEnabled) DrawSprites();
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
		/// Draws the background on the current scanline.
		/// </summary>
		private void DrawBackground()
		{
			byte MapPixelY = (byte)(ScrollY + LY);
			byte MapPixelX = 0;
			for (int LCD_X = 0; LCD_X < LCDWidth; LCD_X++)
			{
				MapPixelX = (byte)(ScrollX + LCD_X);
				byte TileIndex = VRAM[BGTileMapStart + ((MapPixelY >> 3) * 32) + (MapPixelX >> 3)];
				if (isSignedTileIndex) TileIndex += 0x80;
				int TileOffset = BGWinTileDataStart + (TileIndex * 0x10);
				int TilePixelColor = GetPixelPaletteNumberFromTile(TileOffset, MapPixelX & 0x7, MapPixelY & 0x7, false, false);
				SetPixel(LCD_X, LY, BGPalette_DMG[TilePixelColor]);
			}
		}

		/// <summary>
		/// Draws the window on the current scanline.
		/// </summary>
		private void DrawWindow()
		{
			if (LY < WindowY) return;//If scanline is before WY, no drawing is needed.
			if ((WindowX - 7) >= LCDWidth) return;//If WX is past the LCD, no drawing is needed.
			byte MapPixelY = (byte)(LY - WindowY);
			byte MapPixelX = 0;
			for (int LCD_X = (WindowX - 7); LCD_X < LCDWidth; LCD_X++)
			{
				MapPixelX = (byte)(LCD_X - (WindowX - 7));
				byte TileIndex = VRAM[WindowTileMapStart + ((MapPixelY >> 3) * 32) + (MapPixelX >> 3)];
				if (isSignedTileIndex) TileIndex += 0x80;
				int TileOffset = BGWinTileDataStart + (TileIndex * 0x10);
				int TilePixelColor = GetPixelPaletteNumberFromTile(TileOffset, MapPixelX & 0x7, MapPixelY & 0x7, false, false);
				SetPixel(LCD_X, LY, BGPalette_DMG[TilePixelColor]);
			}
		}

		/// <summary>
		/// Draws the sprites on the current scanline.
		/// </summary>
		private void DrawSprites()
		{
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
			int tileXpos = xFlip ? xPos : (7 - xPos);
			int tileYpos = tileOffset + (yFlip ? ((7 - yPos) * 2) : (yPos * 2));
			int plane0 = (VRAM[tileYpos + 0] >> tileXpos) & 1;
			int plane1 = (VRAM[tileYpos + 1] >> tileXpos) & 1;
			return (plane1 << 1) + plane0;
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
		/// <param name="xPos">The X position of the pixel.</param>
		/// <param name="yPos">The Y position of the pixel.</param>
		/// <param name="color">The color of the pixel.</param>
		private void SetPixel(int xPos, int yPos, XnaColor color)
		{
			LCDMap[(yPos * LCDStride) + xPos] = color;
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
			lyCounter.UpdateCounter(cycles);
			if (LCDEnabled)
			{
				CycleCounter += cycles;
				#region Changing modes (need to add special case for drawing scanline 0)
				//Modes go 2 -> 3 -> 0 -> 2 ->...0 -> 1 -> ... -> 1 -> 2
				//LY increases happen after 0, or after each line of 1.
				if (CycleCounter >= TimeToNextModeChange)
				{
					CycleCounter -= TimeToNextModeChange;
					switch (LCDMode)
					{
						case 2://Mode 2: Searching OAM...no access to OAM, progresses to 3
							ChangeLCDMode(3);
							break;
						case 3://Mode 3: Searching OAM/VRAM...no access to OAM/VRAM or Palette Data, progresses to 0
							ChangeLCDMode(0);
							break;
						case 0://Mode 0: HBlank...Access allowed, progresses to 1 (if at end of screen draw), or 2.
							if (LY >= LCDHeight)
							{
								ChangeLCDMode(1);
							}
							else
							{
								ChangeLCDMode(2);
							}
							break;
						case 1://Mode 1: VBlank...access allowed, progresses to 2
							ChangeLCDMode(2);
							break;
					}
				}
				#endregion
			}
			ExecutedFrameCycles += cycles;
			if (ExecutedFrameCycles >= LCDDrawCycles)
			{
				BlitScreen();
				ExecutedFrameCycles = 0;
			}
		}

		public void BlitScreen()
		{
			screen.CopyData(LCDMap);
		}
	}
}
