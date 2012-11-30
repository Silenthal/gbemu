namespace GBEmu.Emulator.Input
{
    using System;
    using GBEmu.Emulator.IO;

    /// <summary>
    /// Represents a button on the Game Boy.
    /// </summary>
    [Flags]
    public enum GBKeys : byte
    {
        Up = 0x40,
        Down = 0x80,
        Left = 0x20,
        Right = 0x10,
        A = 0x1,
        B = 0x2,
        Start = 0x8,
        Select = 0x4
    };

    /// <summary>
    /// Represents the device that handles input.
    /// </summary>
    internal class GBInput : IReadWriteCapable
    {
        private InterruptManager interruptManager;
        private GBKeys keyState;
        private byte LineEnabled = 0x30;

        public GBInput(InterruptManager iM)
        {
            interruptManager = iM;
        }

        public byte Joy1
        {
            get
            {
                byte down2 = (byte)~keyState;
                if (IsDPadReading)
                    down2 >>= 4;
                else if (IsBothReading)
                    down2 = 0x0F;
                down2 &= 0x0F;
                return (byte)(LineEnabled | down2);
            }
            set
            {
                LineEnabled = (byte)(value & 0x30);
            }
        }

        private bool IsBothReading
        {
            get
            {
                return (LineEnabled & 0x30) == 0;
            }
        }

        private bool IsButtonReading
        {
            get
            {
                return (LineEnabled & 0x20) == 0;
            }
        }

        private bool IsDPadReading
        {
            get
            {
                return (LineEnabled & 0x10) == 0;
            }
        }

        /// <summary>
        /// Changes the state of one of the Game Boy's buttons.
        /// </summary>
        /// <param name="key">The key whose state is being changed.</param>
        /// <param name="isDown">Represents whether the button is pressed or not.</param>
        public void KeyChange(GBKeys key, bool isDown)
        {
            if (isDown)
            {
                keyState |= key;
                interruptManager.RequestInterrupt(InterruptType.Joypad);
            }
            else
            {
                keyState &= ~key;
            }
        }

        public byte Read(int position)
        {
            if (position == 0xFF00)
                return Joy1;
            else
                return 0;
        }

        public void Write(int position, byte data)
        {
            if (position == 0xFF00)
                Joy1 = data;
        }
    }
}