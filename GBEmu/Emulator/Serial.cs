using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GBRead.Emulator
{
    class Serial : IODevice
    {
        public byte SerialInterrupt { get { return 0; } }

        public Serial()
        {

        }

        public override byte Read(int position)
        {
            return 0;
        }

        public override void Write(int position, byte data)
        {
            
        }

        public override void UpdateCounter(int cycles)
        {
            
        }
    }
}
