using System.ComponentModel;

namespace GBEmu.EmuTiming.Win32
{
	class HighResTimer : ITimekeeper
	{
		private long startTime, stopTime;
		private long frequency;

		public HighResTimer()
		{
			startTime = 0;
			stopTime  = 0;

			if (UnsafeNativeMethods.QueryPerformanceFrequency(out frequency) == false)
			{
				throw new Win32Exception();
			}
		}

		public void Start()
		{
			UnsafeNativeMethods.QueryPerformanceCounter(out startTime);
			stopTime = 0;
		}

		public void Stop()
		{
			UnsafeNativeMethods.QueryPerformanceCounter(out stopTime);

		}

		public double ElapsedTime()
		{
			long tempTime;
			UnsafeNativeMethods.QueryPerformanceCounter(out tempTime);
			return (double)(tempTime - startTime) / (double)frequency;
		}

		public double Duration()
		{
			return ((double)startTime - (double)stopTime) / (double)frequency;
		}
	}
}
