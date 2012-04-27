using System.Runtime.InteropServices;
using GBEmu.Render;

namespace GBEmu.Emulator
{
	/// <summary>
	/// Represents a color in ARGB format.
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct ABGRColor
	{
		[FieldOffset(0)]
		public uint ABGRVal;
		[FieldOffset(0)]
		public byte Red;
		[FieldOffset(1)]
		public byte Green;
		[FieldOffset(2)]
		public byte Blue;
		[FieldOffset(3)]
		public byte Alpha;
		public static ABGRColor White = new ABGRColor(){Alpha = 255, Blue = 255, Green = 255, Red = 255};
		public static ABGRColor LightGrey = new ABGRColor() { Alpha = 255, Blue = 211, Green = 211, Red = 211 };
		public static ABGRColor DarkGrey = new ABGRColor() { Alpha = 255, Blue = 169, Green = 169, Red = 169 };
		public static ABGRColor Black = new ABGRColor() { Alpha = 255, Blue = 0, Green = 0, Red = 0 };
		public static ABGRColor DMGWhite = new ABGRColor() { Alpha = 255, Blue = 208, Green = 248, Red = 224 };
		public static ABGRColor DMGLightGrey = new ABGRColor() { Alpha = 255, Blue = 112, Green = 192, Red = 136 };
		public static ABGRColor DMGDarkGrey = new ABGRColor() { Alpha = 255, Blue = 86, Green = 104, Red = 52 };
		public static ABGRColor DMGBlack = new ABGRColor() { Alpha = 255, Blue = 32, Green = 24, Red = 8 };
	}

	public class DMGPredefColor
	{
		public static ABGRColor[] Colors = { ABGRColor.DMGWhite, ABGRColor.DMGLightGrey, ABGRColor.DMGDarkGrey, ABGRColor.DMGBlack };
	}

	/// <summary>
	/// Contains information about a sprite in the OAM table.
	/// </summary>
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

	/// <summary>
	/// A manager for the LY counter for the display.
	/// </summary>
	public class LYCounter : TimedIODevice
	{
		public static int LineCycles = 456;
		public byte LY { get; private set; }
		public int TimeOnCurrentLine
		{
			get
			{
				return CycleCounter;
			}
		}
		private bool IsInMode2 = false;

		public delegate void OnLineChangedEventHandler();
		public delegate void OnMode3EventHandler();

		public event OnLineChangedEventHandler LineChanged;
		public event OnMode3EventHandler OnMode3;

		protected virtual void OnLineChanged()
		{
			if (LineChanged != null)
				LineChanged();
		}

		protected virtual void OnMode3Reached()
		{
			if (OnMode3 != null)
				OnMode3();
		}

		public override void UpdateCounter(int cycles)
		{
			CycleCounter += cycles;
			if (IsInMode2 && LY < 144 && CycleCounter >= 80)
			{
				IsInMode2 = false;
				OnMode3();
			}
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
			IsInMode2 = true;
		}

		public void ResetLY()
		{
			LY = 0;
			CycleCounter = 0;
			OnLineChanged();
			IsInMode2 = true;
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
				return ((GetMode() & 0x2) == 0) || !LCDEnabled;
			}
		}
		private bool VRAMAccessAllowed
		{
			get
			{
				return (GetMode() != 3) || !LCDEnabled;
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
		private int SpriteHeight
		{
			get
			{
				return Sprite8By16Mode ? 16 : 8;
			}
		}
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
		private byte stat;
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
		private byte LCDStatus
		{
			get
			{
				return (byte)(stat | GetMode());
			}
			set
			{
				stat = (byte)(value & 0x78);
			}
		}
		#region LCD Status 
		/// <summary>
		/// Indicates whether the LY=LYC coincidence interrupt is used.
		/// </summary>
		private bool LYCoincidenceInterruptEnabled
		{
			get
			{
				return (stat & 0x40) != 0;
			}
			set
			{
				if (value) stat |= 0x40;
				else stat &= 0xBF;
			}
		}//Bit 6
		/// <summary>
		/// Controls whether an interrupt is triggered when the LCD starts to search the OAM table.
		/// </summary>
		private bool Mode2_OAMInterruptEnabled
		{
			get
			{
				return (stat & 0x20) != 0;
			}
			set
			{
				if (value) stat |= 0x20;
				else stat &= 0xDF;
			}
		}//Bit 5
		/// <summary>
		/// Controls whether an interrupt is generated when the display enters V-Blank.
		/// </summary>
		private bool Mode1_VBlankInterruptEnabled
		{
			get
			{
				return (stat & 0x10) != 0;
			}
			set
			{
				if (value) stat |= 0x10;
				else stat &= 0xEF;
			}
		}//Bit 4
		/// <summary>
		/// Controls whether an interrupt is generated when the display enters H-Blank.
		/// </summary>
		private bool Mode0_HBlankInterruptEnabled
		{
			get
			{
				return (stat & 0x08) != 0;
			}
			set
			{
				if (value) stat |= 0x08;
				else stat &= 0xF7;
			}
		}//Bit 3
		/// <summary>
		/// Indicates whether LY is equal to LYC.
		/// </summary>
		private bool LYCoincidence
		{
			get
			{
				return (stat & 0x04) != 0;
			}
			set
			{
				if (value) stat |= 0x04;
				else stat &= 0xFB;
			}
		}//Bit 2
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
		private ABGRColor[] BGPalette_DMG;
		private ABGRColor[][] DMGObjectPalettes;
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
		private byte WinXAdjusted
		{
			get
			{
				return (byte)(WindowX - 7);
			}
		}
		#endregion

		#region Cycle Constants
		private const int Mode1Cycles = 4560;
		private const int Mode2Cycles = 80;
		private int Mode0Cycles
		{
			get
			{
				return 204 - GetSpriteCountOnCurrentScanline();
			}
		}
		private int Mode3Cycles
		{
			get
			{
				return 172 + GetSpriteCountOnCurrentScanline();
			}
		}
		private const int LYOnScreenCycles = 65664;
		private const int LCDDrawCycles = 70224;
		#endregion

		#region Memory Locations
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
		public byte[] OAM;
		#endregion

		/// <summary>
		/// Represents the LCD that is being drawn to.
		/// </summary>
		private uint[] LCDMap;
		
		public int ExecutedFrameCycles;

		private bool CheckMode3Time;
		private int TimeUntilMode0;

		private SpriteInfo[] SpriteInfoTable;

		#region Drawing priority-related structures
		private int[] BackgroundColorNumOnScanline;
		private SpriteInfo[] SpritesOnScanline;
		#endregion

		public Video(InterruptManager iM, IRenderable newScreen)
		{
			screen = newScreen;
			interruptManager = iM;
			lyCounter = new LYCounter();
			lyCounter.LineChanged += new LYCounter.OnLineChangedEventHandler(OnLYLineChange);
			lyCounter.OnMode3 += new LYCounter.OnMode3EventHandler(Mode3Handler);
			ResetLCD();
			InitializeVideoMemory();
			InitializePalettes();
			InitializeLCD();
			CheckMode3Time = false;
		}

		#region Initialization
		private void InitializeLCD()
		{
			LCDMap = new uint[LCDArraySize];
			for (int i = 0; i < LCDMap.Length; i++)
			{
				LCDMap[i] = DMGPredefColor.Colors[1].ABGRVal;
			}
			BackgroundColorNumOnScanline = new int[LCDWidth];
			SpritesOnScanline = new SpriteInfo[LCDWidth];
			LCDControl = 0x91;
			stat = 0x80;
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
			BGPalette_DMG = new ABGRColor[4];
			DMGObjectPalettes = new ABGRColor[2][];
			DMGObjectPalettes[0] = new ABGRColor[4];
			DMGObjectPalettes[0][0] = DMGPredefColor.Colors[0];
			DMGObjectPalettes[1] = new ABGRColor[4];
			DMGObjectPalettes[1][0] = DMGPredefColor.Colors[0];
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
						{
							byte ffs = LCDStatus;
							return LCDStatus;
						}
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
			else
			{
				int x = GetMode();
				return 0xFF;
			}
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
							//True if bit for 'lcdenable' is switched from 0 to 1.
							bool lcdJustTurnedOn = ((LCDControl ^ data) & 0x80) != 0;
							if (lcdJustTurnedOn)
							{
								if ((data & 0x80) != 0)//If LCD was turned on
								{
									ResetLCD();
								}
							}
							LCDControl = data;
						}
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

		public void OAMDMAWrite(int position, byte data)
		{
			OAM[position - 0xFE00] = data;
		}
		#endregion

		#region LY/mode management
		private void OnLYLineChange()
		{
			if (LCDEnabled)
			{
				if (LY < LCDHeight)
				{
					ReconstructOAMTable();
					if (Mode2_OAMInterruptEnabled)
					{
						interruptManager.RequestInterrupt(InterruptType.LCDC);
					}
					else if (LY == LYCompare)
					{
						LYCoincidence = true;
						if (LYCoincidenceInterruptEnabled)
						{
							interruptManager.RequestInterrupt(InterruptType.LCDC);
						}
					}
					else LYCoincidence = false;
				}
				else if (LY == LCDHeight)
				{
					interruptManager.RequestInterrupt(InterruptType.VBlank);
					if (Mode1_VBlankInterruptEnabled)
					{
						interruptManager.RequestInterrupt(InterruptType.LCDC);
					}
					else if (LY == LYCompare)
					{
						LYCoincidence = true;
						if (LYCoincidenceInterruptEnabled)
						{
							interruptManager.RequestInterrupt(InterruptType.LCDC);
						}
					}
					else LYCoincidence = false;
				}
				else
				{
					if (LY == LYCompare)
					{
						LYCoincidence = true;
						if (LYCoincidenceInterruptEnabled)
						{
							interruptManager.RequestInterrupt(InterruptType.LCDC);
						}
					}
					else LYCoincidence = false;
				}
			}
		}

		private void Mode3Handler()
		{
			DrawScanline();
			int TimeSpentInMode3 = lyCounter.TimeOnCurrentLine - Mode2Cycles;
			TimeUntilMode0 = Mode3Cycles - TimeSpentInMode3;
			CheckMode3Time = true;
		}

		private int GetMode()
		{
			if (LCDEnabled)
			{
				if (LY >= LCDHeight)
				{
					return 1;
				}
				int Mode0Time = Mode2Cycles + Mode3Cycles;
				if (lyCounter.TimeOnCurrentLine < Mode2Cycles)
				{
					return 2;
				}
				else if (lyCounter.TimeOnCurrentLine < Mode0Time)
				{
					return 3;
				}
				else
				{
					return 0;
				}
			}
			else
			{
				return 0;
			}
		}

		private void ResetLCD()
		{
			lyCounter.ResetLY();
			CycleCounter = 0;
			ExecutedFrameCycles = 0;
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
				DMGObjectPalettes[0][ColorNumber] = DMGPredefColor.Colors[ShadeIndex];
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
				DMGObjectPalettes[1][ColorNumber] = DMGPredefColor.Colors[ShadeIndex];
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
				SetPixel(LCD_X, LY, ABGRColor.DMGWhite);
			}
		}

		/// <summary>
		/// Draws a black scanline on the LCD.
		/// </summary>
		private void DrawBlackScanline()
		{
			for (int LCD_X = 0; LCD_X < LCDWidth; LCD_X++)
			{
				SetPixel(LCD_X, LY, ABGRColor.DMGBlack);
			}
		}

		/// <summary>
		/// Draws the background on the current scanline.
		/// </summary>
		private void DrawBackground()
		{
			byte MapPixelY = (byte)(ScrollY + LY);
			for (byte LCD_X = 0, MapPixelX = ScrollX; LCD_X < LCDWidth; LCD_X++, MapPixelX++)
			{
				DrawTileMapPixelToLCD(BGTileMapStart, MapPixelX, MapPixelY, LCD_X, LY);
			}
		}

		/// <summary>
		/// Draws the window on the current scanline.
		/// </summary>
		private void DrawWindow()
		{
			if (LY < WindowY) return;//If scanline is before WY, no drawing is needed.
			if (WinXAdjusted >= LCDWidth) return;//If WX is past the LCD, no drawing is needed.
			byte MapPixelY = (byte)(LY - WindowY);
			for (byte LCD_X = WinXAdjusted, MapPixelX = 0; LCD_X < LCDWidth; LCD_X++, MapPixelX++)
			{
				DrawTileMapPixelToLCD(WindowTileMapStart, MapPixelX, MapPixelY, LCD_X, LY);
			}
		}

		/// <summary>
		/// Draws a pixel from a tile map to the LCD.
		/// </summary>
		/// <param name="TileMapStart">The start of the tile map in VRAM.</param>
		/// <param name="MapPixelX">The X offset of the pixel in the map.</param>
		/// <param name="MapPixelY">The Y offset of the pixel in the map.</param>
		/// <param name="LCD_X">The X offset of the pixel in the LCD.</param>
		/// <param name="LCD_Y">The Y offset of the pixel in the LCD.</param>
		private void DrawTileMapPixelToLCD(int TileMapStart, byte MapPixelX, byte MapPixelY, byte LCD_X, byte LCD_Y)
		{
			byte TileIndex = VRAM[TileMapStart + ((MapPixelY >> 3) * 32) + (MapPixelX >> 3)];
			if (isSignedTileIndex) TileIndex += 0x80;
			int TileOffset = BGWinTileDataStart + (TileIndex * 0x10);
			int TilePixelColor = GetPixelPaletteNumberFromTile(TileOffset, MapPixelX & 0x7, MapPixelY & 0x7, false, false);
			BackgroundColorNumOnScanline[LCD_X] = TilePixelColor;
			SetPixel(LCD_X, LCD_Y, BGPalette_DMG[TilePixelColor]);
		}

		/// <summary>
		/// Draws the sprites on the current scanline.
		/// </summary>
		private void DrawSprites()
		{
			int LineSpriteCount = 0;
			for (int i = 0; i < SpriteInfoTable.Length; i++)
			{
				SpriteInfo currentSprite = SpriteInfoTable[i];
				if (LY >= currentSprite.YOffset && LY < currentSprite.YOffset + SpriteHeight) //If the sprite intersects LY...
				{
					LineSpriteCount++;//Increment the sprite count for the line.
					if (!currentSprite.IsOnScreen) continue;
					int SpritePixelY = LY - currentSprite.YOffset;
					int SpriteTileOffset = 0;
					if (Sprite8By16Mode)
					{
						if (SpritePixelY < 8)
						{
							SpriteTileOffset = currentSprite.UpperTileOffset;
						}
						else
						{
							SpritePixelY -= 8;
							SpriteTileOffset = currentSprite.LowerTileOffset;
						}
					}
					else
					{
						SpriteTileOffset = currentSprite.TileOffset;
					}
					for (int LCD_X = currentSprite.XOffset, SpritePixelX = 0; LCD_X < currentSprite.XOffset + 8; LCD_X++, SpritePixelX++)
					{
						if (LCD_X >= LCDWidth) break;
						int SpriteColorNum = GetPixelPaletteNumberFromTile(SpriteTileOffset, SpritePixelX, SpritePixelY, currentSprite.XFlip, currentSprite.YFlip);
						//Color 0 is never drawn (transparent).
						//If PriorityOverBG, sprite pixel is drawn over BG, except in the case of sprite color 0.
						//If !PriorityOverBG, sprite pixel isn't drawn over BG, except when BG color is 0.
						if (SpriteColorNum == 0) continue;
						bool PriorityOverExistingSprite = (currentSprite.XOffset < SpritesOnScanline[LCD_X].XOffset) || (currentSprite.OAMIndex < SpritesOnScanline[LCD_X].OAMIndex);
						if (PriorityOverExistingSprite && (currentSprite.PriorityOverBG || BackgroundColorNumOnScanline[LCD_X] == 0))
						{
							SetPixel(LCD_X, LY, DMGObjectPalettes[currentSprite.DMGObjectPaletteNum][SpriteColorNum]);
							SpritesOnScanline[LCD_X] = currentSprite;
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
				if (LY >= SpriteInfoTable[i].YOffset && LY < SpriteInfoTable[i].YOffset + SpriteHeight) spriteCount++;
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
				int off = i << 2;
				SpriteInfoTable[i].OAMIndex			= off;
				SpriteInfoTable[i].YOffset			= OAM[off + 0] - 16;
				SpriteInfoTable[i].XOffset			= OAM[off + 1] - 8;
				SpriteInfoTable[i].TileOffset		= OAM[off + 2] << 4;//index * 16 = offset in VRAM of tile
				SpriteInfoTable[i].SpriteProperties = OAM[off + 3];
				//-Sprites are sorted by their X position. In the case that their Xs are equal,
				//-the one with the lower OAM position takes priority. Because they are already
				//'sorted by OAM offset', only Xs need to be compared.
				int x = i;
				while (x > 0 && (SpriteInfoTable[x].XOffset < SpriteInfoTable[x - 1].XOffset))
				{
					SpriteInfo temp = SpriteInfoTable[x - 1];
					SpriteInfoTable[x - 1] = SpriteInfoTable[x];
					SpriteInfoTable[x] = temp;
					x--;
				}
			}
			for (int i = 0; i < SpritesOnScanline.Length; i++)
			{
				SpritesOnScanline[i] = new SpriteInfo() { OAMIndex = int.MaxValue, XOffset = int.MaxValue, YOffset = int.MaxValue };
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
		private void SetPixel(int xPos, int yPos, ABGRColor color)
		{
			LCDMap[(yPos * LCDStride) + xPos] = color.ABGRVal;
		}

		/// <summary>
		/// Gets the pixel at the specified location in the LCD.
		/// </summary>
		/// <param name="x">The X position of the pixel.</param>
		/// <param name="y">THe Y position of the pixel.</param>
		/// <returns>The pixel at the location.</returns>
		private ABGRColor GetPixel(int x, int y)
		{
			return new ABGRColor() { ABGRVal = LCDMap[(x * LCDStride) + y] };
		}
		#endregion

		public override void UpdateCounter(int cycles)
		{
			lyCounter.UpdateCounter(cycles);
			CycleCounter += cycles;
			if (CheckMode3Time)
			{
				TimeUntilMode0 -= cycles;
				if (TimeUntilMode0 < 0)
				{
					TimeUntilMode0 = int.MaxValue;
					CheckMode3Time = false;
					if (Mode0_HBlankInterruptEnabled)
					{
						interruptManager.RequestInterrupt(InterruptType.LCDC);
					}
				}
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
			screen.CopyFrameData(LCDMap);
		}
	}
}
