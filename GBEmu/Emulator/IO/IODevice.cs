namespace GBEmu.Emulator.IO
{
    /// <summary>
    /// Represents a system device that runs on a clock. Any updates
    /// to this device are handled in UpdateTime.
    /// </summary>
    public abstract class TimedIODevice : IReadWriteCapable
    {
        /// <summary>
        /// A counter to keep track of cycles of time that have passed.
        /// </summary>
        protected int CycleCounter = 0;

        /// <summary>
        /// Updates the device time to reflect the cycles passed.
        /// </summary>
        /// <param name="cycles">The amount of time that has passed.</param>
        public abstract void UpdateTime(int cycles);

        /// <summary>
        /// Reads a byte from an address in memory.
        /// </summary>
        /// <param name="position">The address to read from.</param>
        /// <returns>The data at the address.</returns>
        public abstract byte Read(int position);

        /// <summary>
        /// Writes a byte to an address in memory.
        /// </summary>
        /// <param name="position">The address to write to.</param>
        /// <param name="data">The data to write.</param>
        public abstract void Write(int position, byte data);
    }
}