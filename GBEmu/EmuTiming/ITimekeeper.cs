namespace GBEmu.EmuTiming
{
	interface ITimekeeper
	{
		void Start();
		void Stop();
		double Duration();
		double ElapsedTime();
	}
}
