using GBEmu.Emulator.Debug;
using GBEmu.Emulator.Timing;
using System.Collections.Generic;

namespace GBEmu.Emulator.Cartridge
{
    /// <summary>
    /// A factory class for creating Cartridges.
    /// </summary>
    public static class CartLoader
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

        #endregion List of cart features

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

            #endregion MBC

            Logger.GetInstance().Log(new LogMessage(LogMessageSource.Cart, GlobalTimer.GetInstance().GetTime(), "Cart type " + cs + " loaded."));
            return returnedCart;
        }
    }
}