using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GBEmu.Emulator
{
    class SysEventManager
    {
        #region CPU Constants
        public const UInt32 DMGCycles = 4194304;
        public const UInt32 CGB_SingleCycles = 4194300;
        public const UInt32 SGB_Cycles = 4295454;
        public const UInt32 CGB_DoubleCycles = 8338000;
        #endregion

        #region LCD Constants
        public const int MODE_0_CYCLES = 204;
        public const int MODE_1_CYCLES = 1140;
        public const int MODE_2_CYCLES = 80;
        public const int MODE_3_CYCLES = 172;
        #endregion

        public const int LY_CYCLE = 456;
        public const int VBLANK_CYCLES = 4560;
        public const int SCREEN_DRAW_CYCLES = 70224;
        public const int LY_ONSCREEN_CYCLES = 65664;
        public const int CYCLES_PER_SECOND = 4194304;
        public const int DIV_CYCLE = 256;
        public const int DMA_CYCLE = 670;

        public UInt32 LastCycleCount { get; set; }

        public SysEventManager()
        {

        }

        public void UpdateEvents(UInt32 cycleCount)
        {
            LastCycleCount += (cycleCount - LastCycleCount);
        }




    }
}
