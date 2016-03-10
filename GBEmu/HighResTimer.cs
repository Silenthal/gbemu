namespace GBEmu
{
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Security;
    using GBEmu.Emulator.Timing;

    internal class HighResTimer : ITimekeeper
    {
        private long startTime = 0;
        private long stopTime = 0;
        private long duration = 0;
        private long frequency = 0;
        private bool running = false;

        public HighResTimer()
        {
            if (!UnsafeNativeMethods.QueryPerformanceFrequency(out frequency))
            {
                throw new Win32Exception();
            }
        }

        public void Start()
        {
            UnsafeNativeMethods.QueryPerformanceCounter(out startTime);
            running = true;
        }

        public void Restart()
        {
            duration = 0;
            Start();
        }

        public void Stop()
        {
            UnsafeNativeMethods.QueryPerformanceCounter(out stopTime);
            duration += stopTime - startTime;
            running = false;
        }

        public double ElapsedSeconds()
        {
            if (running)
            {
                UnsafeNativeMethods.QueryPerformanceCounter(out stopTime);
                return (double)(duration + (stopTime - startTime)) / (double)frequency;
            }
            else
            {
                return (double)duration / (double)frequency;
            }
        }

        private static class UnsafeNativeMethods
        {
            /// <summary>
            /// Retrieves the current value of the high-resolution performance counter.
            /// </summary>
            /// <param name="lpPerformanceCount">A pointer to a variable that receives the current performance-counter value, in counts.</param>
            /// <returns>If the function succeeds, the return value is nonzero.</returns>
            [SuppressUnmanagedCodeSecurity]
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("kernel32.dll")]
            public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

            /// <summary>
            /// Retrieves the frequency of the high-resolution performance counter, if one exists. The frequency cannot change while the system is running.
            /// </summary>
            /// <param name="lpFrequency">A pointer to a variable that receives the current performance-counter frequency, in counts per second.</param>
            /// <returns>Returns true if the installed hardware supports a high-resolution performance counter.</returns>
            [SuppressUnmanagedCodeSecurity]
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("kernel32.dll")]
            public static extern bool QueryPerformanceFrequency(out long lpFrequency);
        }

        private enum RunState
        {
            Stopped,
            Running
        }
    }
}