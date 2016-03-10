using System.Runtime.InteropServices;

namespace GBEmu.Emulator
{
    /// <summary>
    /// Describes a register pair in the CPU.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RegisterPair
    {
        /// <summary>
        /// The word value of the pair.
        /// </summary>
        [FieldOffset(0)]
        public ushort w;

        /// <summary>
        /// The low value of the pair.
        /// </summary>
        [FieldOffset(0)]
        public byte lo;

        /// <summary>
        /// The high value of the pair.
        /// </summary>
        [FieldOffset(1)]
        public byte hi;
    }
}