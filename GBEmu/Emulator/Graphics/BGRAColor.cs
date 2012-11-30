namespace GBEmu.Emulator.Graphics
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Represents a color in BGRA format.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct BGRAColor
    {
        [FieldOffset(0)]
        public uint Value;

        [FieldOffset(0)]
        public byte Blue;

        [FieldOffset(1)]
        public byte Green;

        [FieldOffset(2)]
        public byte Red;

        [FieldOffset(3)]
        public byte Alpha;

        public static BGRAColor White = new BGRAColor() {
            Alpha = 255, Blue = 255, Green = 255, Red = 255
        };

        public static BGRAColor LightGrey = new BGRAColor() {
            Alpha = 255, Blue = 211, Green = 211, Red = 211
        };

        public static BGRAColor DarkGrey = new BGRAColor() {
            Alpha = 255, Blue = 169, Green = 169, Red = 169
        };

        public static BGRAColor Black = new BGRAColor() {
            Alpha = 255, Blue = 0, Green = 0, Red = 0
        };

        public static BGRAColor DMGWhite = new BGRAColor() {
            Alpha = 255, Blue = 208, Green = 248, Red = 224
        };

        public static BGRAColor DMGLightGrey = new BGRAColor() {
            Alpha = 255, Blue = 112, Green = 192, Red = 136
        };

        public static BGRAColor DMGDarkGrey = new BGRAColor() {
            Alpha = 255, Blue = 86, Green = 104, Red = 52
        };

        public static BGRAColor DMGBlack = new BGRAColor() {
            Alpha = 255, Blue = 32, Green = 24, Red = 8
        };
    }
}