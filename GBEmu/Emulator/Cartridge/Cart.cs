using System;
using System.Collections.Generic;

namespace GBEmu.Emulator.Cartridge
{
	/// <summary>
	/// Represents a type of cartridge, specified in the cartridge header.
	/// </summary>
	public enum CartridgeType : byte
	{
		ROM			= 0x00,

		MBC1		= 0x01,
		MBC1_R		= 0x02,
		MBC1_RB		= 0x03,
		
		MBC2		= 0x05,
		MBC2_B		= 0x06,
		
		ROM_R		= 0x08,
		ROM_RB		= 0x09,
		
		// MMM01		= 0x0B,
		// MMM01_R		= 0x0C,
		// MMM01_RB	= 0x0D,
		
		MBC3_TB		= 0x0F,
		MBC3_TRB	= 0x10,
		MBC3		= 0x11,
		MBC3_R		= 0x12,
		MBC3_RB		= 0x13,
		
		MBC4		= 0x15,
		MBC4_R		= 0x16,
		MBC4_RB		= 0x17,
		
		MBC5		= 0x19,
		MBC5_R		= 0x1A,
		MBC5_RB		= 0x1B,
		MBC5_M		= 0x1C,
		MBC5_MR		= 0x1D,
		MBC5_MRB	= 0x1E,
		
		//PC			= 0xFC,
		//TAMA5		= 0xFD,
		//HuC3		= 0xFE,
		//HuC1_RB		= 0xFF
	}

	/// <summary>
	/// Represents features present in the cart.
	/// </summary>
	[Flags]
	public enum CartFeatures : byte
	{
		None = 0x0,
		RAM = 0x1,
		BatteryBacked = 0x2,
		Timer = 0x4,
		Rumble = 0x8,
	}

	/// <summary>
	/// A factory class for creating Cartridges.
	/// </summary>
	public class CartLoader
	{
		#region List of cart features
		private static Dictionary<CartridgeType, CartFeatures> FeatureList = new Dictionary<CartridgeType, CartFeatures>()
		{
			{CartridgeType.MBC1, CartFeatures.None}, 
			{CartridgeType.MBC1_R, CartFeatures.RAM}, 
			{CartridgeType.MBC1_RB, CartFeatures.RAM | CartFeatures.BatteryBacked}, 
			{CartridgeType.MBC2, CartFeatures.None}, 
			{CartridgeType.MBC2_B, CartFeatures.BatteryBacked},
			{CartridgeType.MBC3, CartFeatures.None}, 
			{CartridgeType.MBC3_R, CartFeatures.RAM}, 
			{CartridgeType.MBC3_RB, CartFeatures.RAM | CartFeatures.BatteryBacked}, 
			{CartridgeType.MBC3_TB, CartFeatures.Timer | CartFeatures.BatteryBacked}, 
			{CartridgeType.MBC3_TRB, CartFeatures.Timer | CartFeatures.RAM | CartFeatures.BatteryBacked}, 
			{CartridgeType.MBC5, CartFeatures.None}, 
			{CartridgeType.MBC5_M, CartFeatures.Rumble}, 
			{CartridgeType.MBC5_MR, CartFeatures.Rumble | CartFeatures.RAM}, 
			{CartridgeType.MBC5_MRB, CartFeatures.Rumble | CartFeatures.RAM | CartFeatures.BatteryBacked}, 
			{CartridgeType.MBC5_R, CartFeatures.RAM}, 
			{CartridgeType.MBC5_RB, CartFeatures.RAM | CartFeatures.BatteryBacked}, 
			{CartridgeType.ROM, CartFeatures.None}, 
			{CartridgeType.ROM_R, CartFeatures.RAM}, 
			{CartridgeType.ROM_RB, CartFeatures.RAM | CartFeatures.BatteryBacked}
		};
		#endregion
		/// <summary>
		/// Returns the proper Cart object, based on the file loaded.
		/// </summary>
		/// <param name="romFile">The binary file to load.</param>
		/// <returns>A Cart with the characteristics of the cartridge the file specifies.</returns>
		public static Cart LoadCart(byte[] romFile)
		{
			CartridgeType cs = (CartridgeType)romFile[0x147];
			Cart returnedCart = null;
			#region MBC
			switch (cs)
			{
				case CartridgeType.ROM:
				case CartridgeType.ROM_R:
				case CartridgeType.ROM_RB:
					returnedCart = new PlainCart(romFile, FeatureList[cs]);
					break;
				case CartridgeType.MBC1:
				case CartridgeType.MBC1_R:
				case CartridgeType.MBC1_RB:
					returnedCart = new MBC1(romFile, FeatureList[cs]);
					break;
				case CartridgeType.MBC2:
				case CartridgeType.MBC2_B:
					returnedCart = new MBC2(romFile, FeatureList[cs]);
					break;
				case CartridgeType.MBC3:
				case CartridgeType.MBC3_TB:
				case CartridgeType.MBC3_TRB:
				case CartridgeType.MBC3_R:
				case CartridgeType.MBC3_RB:
					returnedCart = new MBC3(romFile, FeatureList[cs]);
					break;
				case CartridgeType.MBC5:
				case CartridgeType.MBC5_M:
				case CartridgeType.MBC5_MR:
				case CartridgeType.MBC5_MRB:
				case CartridgeType.MBC5_R:
				case CartridgeType.MBC5_RB:
					returnedCart = new MBC5(romFile, FeatureList[cs]);
					break;
				default:
					throw new System.Exception("Cart is not supported.");
			}
			#endregion
			return returnedCart;
		}
	}

	public abstract class Cart : IReadWriteCapable
	{
		protected byte[] romFile;
		protected int MaxRamBank;

		protected bool RamEnabled = false;
		protected int RomBank;
		protected int MaxRomBank;

		protected CartFeatures features;

		protected bool BatteryPresent { get { return (features & CartFeatures.BatteryBacked) == CartFeatures.BatteryBacked; } }
		protected bool RumblePresent { get { return (features & CartFeatures.Rumble) == CartFeatures.Rumble; } }
		protected bool TimerPresent { get { return (features & CartFeatures.Timer) == CartFeatures.Timer; } }

		protected byte[] CartRam;
		protected int CartRamBank;

		protected Cart(byte[] inFile, CartFeatures cartFeatures)
		{
			features = cartFeatures;
			romFile = new byte[inFile.Length];
			Array.Copy(inFile, romFile, inFile.Length);
			MaxRomBank = romFile.Length >> 14;
			InitializeOutsideRAM();
			RomBank = 1;
		}

		protected virtual void InitializeOutsideRAM()
		{
			CartRamBank = 0;
			MaxRamBank = 0;
			switch (romFile[0x149])
			{
				case 0x00://No RAM?
					MaxRamBank = 2;
					break;
				case 0x01://A000-A7FF, 2 kilobytes
				case 0x02://A000-BFFF, 8 kilobytes
					MaxRamBank = 1;
					break;
				case 0x03://A000-BFFF x 4, 32 kilobytes
					MaxRamBank = 4;
					break;
				default:
					MaxRamBank = 16;
					break;
			}
			CartRam = new byte[MaxRamBank * 0x2000];
		}

		public byte Read(int position)
		{
			position &= 0xFFFF;
			if (position < 0x4000)
			{
				return romFile[position];
			}
			else if (position < 0x8000)
			{
				return romFile[(RomBank << 14) | (position & 0x3FFF)];
			}
			else if (position >= 0xA000 && position < 0xC000)
			{
				return CartRamRead(position);
			}
			else
			{
				return 0xFF;
			}
		}

		protected virtual byte CartRamRead(int position)
		{
			position -= 0xA000;
			if (RamEnabled)
			{
				return CartRam[(CartRamBank << 13) | position];
			}
			else
			{
				return 0xFF;
			}
		}

		public void Write(int position, byte value)
		{
			position &= 0xFFFF;
			if (position < 0x8000)
			{
				MBCWrite(position, value);
			}
			else if (position >= 0xA000 && position < 0xC000)
			{
				CartRamWrite(position, value);
			}
		}

		protected abstract void MBCWrite(int position, byte value);
			
		protected virtual void CartRamWrite(int position, byte value)
		{
			if (RamEnabled)
			{
				if ((position - 0xA000) >= CartRam.Length)
				{
					return;
				}
				CartRam[(CartRamBank * 0x2000) + (position - 0xA000)] = value;
			}
		}
	}
}
