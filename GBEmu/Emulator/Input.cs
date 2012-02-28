using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GBEmu.Emulator
{
    public enum GBKeys : byte { Up = 0x14, Down = 0x18, Left = 0x12, Right = 0x11, A = 0x1, B = 0x2, Start = 0x8, Select = 0x4 };
    class Input : IODevice
    {
        private byte LineEnabled = 0x30;
        public byte Joy1
        {
            get
            {
                return (byte)(LineEnabled | (LineEnabled == 0x20 ? LineDPad : LineEnabled == 0x10 ? LineButtons : LineDefault));
            }
            set
            {
                LineEnabled = (byte)(value & 0x30);
            }
        }

        private byte LineDPad = 0x0F;
        private byte LineButtons = 0x0F;
        private static byte LineDefault = 0x0F;

        public bool LineHit
        {
            get
            {
                return (LineDPad != LineDefault || LineButtons != LineDefault);
            }
        }

        public byte JoyInterrupt { get { return (byte)(LineHit ? 0x10 : 0); } }

        public Input()
        {
            
        }

        public override byte Read(int position)
        {
            if (position == 0xFF00) return Joy1;
            else return 0;
        }

        public override void Write(int position, byte data)
        {
            if (position == 0xFF00) Joy1 = data;
        }

        public override void UpdateCounter(int cycles)
        {
            
        }

        public void KeyChange(GBKeys key, bool isDPad, bool isDown)
        {
            byte keyVal = (byte)((byte)key & 0x0F);
            if (isDPad)
            {
                if (isDown) LineDPad |= keyVal;
                else LineDPad &= (byte)~keyVal;
            }
            else
            {
                if (isDown) LineButtons |= keyVal;
                else LineButtons &= (byte)~keyVal;
            }
        }
    }
}
