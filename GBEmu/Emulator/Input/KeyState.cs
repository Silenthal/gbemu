namespace GBEmu.Emulator.Input
{
    public class KeyState
    {
        public byte dpadState = 0x0F;
        public byte buttonState = 0x0F;
        public int emulatorKeyState = 0;

        public bool AreKeysTriggered
        {
            get
            {
                return ((buttonState & dpadState & 0x0F) != 0x0F);
            }
        }

        public bool IsADown
        {
            get
            {
                return (~buttonState & 0x1) != 0;
            }
            set
            {
                if (value)
                {
                    buttonState &= 0xFE;
                }
                else
                {
                    buttonState |= 0x1;
                }
            }
        }

        public bool IsBDown
        {
            get
            {
                return (~buttonState & 0x2) != 0;
            }
            set
            {
                if (value)
                {
                    buttonState &= 0xFD;
                }
                else
                {
                    buttonState |= 0x2;
                }
            }
        }

        public bool IsSelectDown
        {
            get
            {
                return (~buttonState & 0x4) != 0;
            }
            set
            {
                if (value)
                {
                    buttonState &= 0xFB;
                }
                else
                {
                    buttonState |= 0x4;
                }
            }
        }

        public bool IsStartDown
        {
            get
            {
                return (~buttonState & 0x8) != 0;
            }
            set
            {
                if (value)
                {
                    buttonState &= 0xF7;
                }
                else
                {
                    buttonState |= 0x8;
                }
            }
        }

        public bool IsRightDown
        {
            get
            {
                return (~dpadState & 0x1) != 0;
            }
            set
            {
                if (value)
                {
                    dpadState &= 0xFE;
                }
                else
                {
                    dpadState |= 0x1;
                }
            }
        }

        public bool IsLeftDown
        {
            get
            {
                return (~dpadState & 0x2) != 0;
            }
            set
            {
                if (value)
                {
                    dpadState &= 0xFD; 
                }
                else
                {
                    dpadState |= 0x2;
                }
            }
        }

        public bool IsUpDown
        {
            get
            {
                return (~dpadState & 0x4) != 0;
            }
            set
            {
                if (value)
                {
                    dpadState &= 0xFB;
                }
                else
                {
                    dpadState |= 0x4;
                }
            }
        }

        public bool IsDownDown
        {
            get
            {
                return (~dpadState & 0x8) != 0;
            }
            set
            {
                if (value)
                {
                    dpadState &= 0xF7;
                }
                else
                {
                    dpadState |= 0x8;
                }
            }
        }

        public bool IsSaveState1Down
        {
            get
            {
                return (emulatorKeyState & 0x1) != 0;
            }
            set
            {
                if (value)
                {
                    emulatorKeyState |= 0x1;
                }
                else
                {
                    emulatorKeyState &= ~0x1;
                }
            }
        }

        public bool IsPauseToggled
        {
            get
            {
                return (emulatorKeyState & 0x2) != 0;
            }
            set
            {
                if (value)
                {
                    emulatorKeyState |= 0x2;
                }
                else
                {
                    emulatorKeyState &= ~0x2;
                }
            }
        }

        public bool IsFrameLimitToggled
        {
            get
            {
                return (emulatorKeyState & 0x4) != 0;
            }
            set
            {
                if (value)
                {
                    emulatorKeyState |= 0x4;
                }
                else
                {
                    emulatorKeyState &= ~0x4;
                }
            }
        }
    }
}