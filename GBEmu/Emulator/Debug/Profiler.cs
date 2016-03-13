using GBEmu.Emulator.Timing;
using System;
using System.Collections.Generic;

namespace GBEmu.Emulator.Debug
{
    public class Profiler
    {
        private static readonly Lazy<Profiler> instance = new Lazy<Profiler>(() => new Profiler());
        private const double frameTime = 1d / 60d;
        private Dictionary<string, ITimekeeper> timeKeeper = new Dictionary<string, ITimekeeper>();

        private Profiler()
        {
        }

        public static Profiler GetInstance()
        {
            return instance.Value;
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