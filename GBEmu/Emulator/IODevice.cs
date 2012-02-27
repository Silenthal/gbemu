using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GBRead.Emulator
{
    abstract class IODevice
    {
        protected int CycleCounter = 0;
        public abstract byte Read(int position);
        public abstract void Write(int position, byte data);
        public abstract void UpdateCounter(int cycles);
    }
}
