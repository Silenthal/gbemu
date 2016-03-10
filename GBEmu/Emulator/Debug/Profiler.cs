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
            if (!timeKeeper.ContainsKey(profile))
            {
                timeKeeper.Add(profile, new HighResTimer());
            }
            timeKeeper[profile].Start();
        }

        public void Restart(string profile)
        {
            if (!timeKeeper.ContainsKey(profile))
            {
                timeKeeper.Add(profile, new HighResTimer());
            }
            timeKeeper[profile].Restart();
        }

        public void Stop(string profile)
        {
            if (!timeKeeper.ContainsKey(profile))
            {
                timeKeeper.Add(profile, new HighResTimer());
            }
            timeKeeper[profile].Stop();
        }

        public double StopAndGetTimeInSeconds(string profile)
        {
            if (timeKeeper.ContainsKey(profile))
            {
                timeKeeper[profile].Stop();
                return timeKeeper[profile].ElapsedSeconds();
            }
            else
            {
                return -1;
            }
        }

        public double StopAndGetTimeAsFrameTimePercent(string profile)
        {
            if (timeKeeper.ContainsKey(profile))
            {
                timeKeeper[profile].Stop();
                return (timeKeeper[profile].ElapsedSeconds() / frameTime) * 100d;
            }
            else
            {
                return -1;
            }
        }

        public double GetTimeInSeconds(string profile)
        {
            if (timeKeeper.ContainsKey(profile))
            {
                return timeKeeper[profile].ElapsedSeconds();
            }
            else
            {
                return -1;
            }
        }

        public double GetTimeAsFrameTimePercent(string profile)
        {
            if (timeKeeper.ContainsKey(profile))
            {
                return (frameTime / timeKeeper[profile].ElapsedSeconds()) * 100;
            }
            else
            {
                return -1;
            }
        }
    }
}