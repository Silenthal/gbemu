using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GBRead.Emulator
{
    class Audio : IODevice
    {
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
