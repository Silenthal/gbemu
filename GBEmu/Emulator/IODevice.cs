namespace GBEmu.Emulator
{
	public interface IReadWriteCapable
	{
		byte Read(int position);
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
