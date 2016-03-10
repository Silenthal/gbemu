namespace GBEmu.Emulator.Timing
{
    using GBEmu.Emulator.IO;

    internal class Divider : TimedIODevice
    {
        /// <summary>
        /// Contains the cycles that occur before the divider increments.
        /// </summary>
        /// <remarks>
        /// For DMG, there are 4194304 cycles/second.
        /// The Divider ticks at 16384 Hz in GB mode.
        /// So, 256 cycles * 16384 times.
        /// </remarks>
        private const int DIV_CYCLE = 256;

        public byte DIV_Divider
        {
            get;
            private set;
        }

        public Divider()
        {
            InitializeDefaultValues();
        }

        private void InitializeDefaultValues()
        {
            DIV_Divider = 0x1C;
        }

        public override byte Read(int position)
        {
            if (position == 0xFF04)
            {
                return DIV_Divider;
            }
            return 0xFF;
        }

        public override void Write(int position, byte data)
        {
            if (position == 0xFF04)
            {
                InitializeDefaultValues();
            }
        }

        public override void UpdateTime(int cycles)
        {
            CycleCounter += cycles;
            if (CycleCounter >= DIV_CYCLE)
            {
                CycleCounter -= DIV_CYCLE;
                DIV_Divider++;
            }
        }
    }
}