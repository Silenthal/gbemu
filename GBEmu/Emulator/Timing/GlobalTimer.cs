using System;

namespace GBEmu.Emulator.Timing
{
    public class GlobalTimer
    {
        private long counter = 0;
        private long eventCounter = 0;
        private static readonly Lazy<GlobalTimer> instance = new Lazy<GlobalTimer>(() => new GlobalTimer());

        private GlobalTimer()
        {
        }

        public void Increment(long amount)
        {
            counter += amount;
            eventCounter += amount;
        }

        public long GetTime()
        {
            return counter;
        }

        public long GetEventCounter()
        {
            return eventCounter;
        }

        public void ResetEventCounter()
        {
            eventCounter = 0;
        }

        public static GlobalTimer GetInstance()
        {
            return instance.Value;
        }
    }
}