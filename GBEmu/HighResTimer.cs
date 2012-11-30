namespace GBEmu
{
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Security;
    using GBEmu.Emulator.Timing;

    internal class HighResTimer : ITimekeeper
    {
        private long startTime, stopTime;
        private long frequency;

        public HighResTimer()
        {
            startTime = 0;
            stopTime = 0;

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
    }
}