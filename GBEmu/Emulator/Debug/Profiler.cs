namespace GBEmu.Emulator.Debug
{
    using System.Collections.Generic;
    using GBEmu.Emulator.Timing;

    internal class Profiler
    {
        private static Profiler instance = new Profiler();
        private const double frameTime = 1d / 60d;
        private Dictionary<string, ITimekeeper> timeKeeper = new Dictionary<string, ITimekeeper>();

        private Profiler()
        {
        }

        public static Profiler GetInstance()
        {
            return instance;
        }

        public void Start(string profile)
        {
            timeKeeper.Add(profile, new HighResTimer());
            timeKeeper[profile].Start();
        }

        public double StopAndFetchTime(string profile)
        {
            if (timeKeeper.ContainsKey(profile))
            {
                timeKeeper[profile].Stop();
                double ret = (timeKeeper[profile].ElapsedTime() / frameTime) * 100d;
                timeKeeper.Remove(profile);
                return ret;
            }
            else
            {
                return -1;
            }
        }
    }
}