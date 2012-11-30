namespace GBEmu.Emulator.IO
{
    /// <summary>
    /// Describes a read/write interface for a system device.
    /// </summary>
    internal interface IReadWriteCapable
    {
        /// <summary>
        /// Reads a byte from an address in memory.
        /// </summary>
        /// <param name="position">The address to read from.</param>
        /// <returns>The data at the address.</returns>
        byte Read(int position);
        /// <summary>
        /// Writes a byte to an address in memory.
        /// </summary>
        /// <param name="position">The address to write to.</param>
        /// <param name="data">The data to write.</param>
        void Write(int position, byte data);
    }
}
