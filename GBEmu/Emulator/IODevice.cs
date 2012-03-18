namespace GBEmu.Emulator
{
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
