using GBEmu.Emulator.Timing;
using System.Diagnostics;

namespace GBEmu
{
    public class HighResTimer : ITimekeeper
    {
        private Stopwatch sw;

        public HighResTimer()
        {
            sw = new Stopwatch();
        }

        public void Start()
        {
            sw.Start();
        }

        public void Restart()
        {
            sw.Restart();
        }

        public void Stop()
        {
            sw.Stop();
        }

        public double ElapsedSeconds()
        {
            return sw.ElapsedMilliseconds / 1000;
        }
    }
}