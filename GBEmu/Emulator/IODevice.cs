namespace GBEmu.Emulator
{
	/// <summary>
	/// Reads a byte from the MMU.
	/// </summary>
	/// <param name="address">The address to read from.</param>
	/// <returns>The contents of the memory location.</returns>
	public delegate byte ReadFromMMUDelegate(int address);
	/// <summary>
	/// Writes a byte to the MMU.
	/// </summary>
	/// <param name="address">The address to write to.</param>
	/// <param name="data">The data to write.</param>
	public delegate void WriteToMMUDelegate(int address, byte data);
	/// <summary>
	/// Updates the system time to reflect the cycles passed.
	/// </summary>
	/// <param name="cycles">The amount of time that has passed.</param>
	public delegate void UpdateTimeDelegate(int cycles);

	public interface IReadWriteCapable
	{
		/// <summary>
		/// Reads a byte from memory.
		/// </summary>
		/// <param name="position">The offset to be read from.</param>
		/// <returns>The data at the position.</returns>
		byte Read(int position);
		/// <summary>
		/// Writes a byte to a particular position in memory.
		/// </summary>
		/// <param name="position">The address to write to.</param>
		/// <param name="data">The data to write.</param>
		void Write(int position, byte data);
	}
	public abstract class TimedIODevice : IReadWriteCapable
	{
		protected int CycleCounter = 0;
		public abstract void UpdateCounter(int cycles);
		public abstract byte Read(int position);
		public abstract void Write(int position, byte data);
	}
}
