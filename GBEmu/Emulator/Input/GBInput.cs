namespace GBEmu.Emulator.Input
{
    using System;
    using GBEmu.Emulator.IO;

    /// <summary>
    /// Represents the device that handles input.
    /// </summary>
    internal class GBInput : IReadWriteCapable
    {
        private InterruptManager interruptManager;
        private IInputHandler inputHandler;
        private byte LineReadState = 0x30;
        private KeyState keyState;

        public GBInput(InterruptManager iM, IInputHandler handler)
        {
            interruptManager = iM;
            inputHandler = handler;
            keyState = new KeyState();
        }

        public byte Joy1
        {
            get
            {
                byte down2 = 0x0F;
                if (IsDPadBeingRead)
                {
                    down2 = keyState.dpadState;
                }
                else if (IsButtonBeingRead)
                {
                    down2 = keyState.buttonState;
                }
                return (byte)(LineReadState | down2);
            }
            set
            {
                LineReadState = (byte)(value & 0x30);
            }
        }

        private bool IsBothBeingRead
        {
            get
            {
                return (LineReadState & 0x30) == 0;
            }
        }

        private bool IsButtonBeingRead
        {
            get
            {
                return (LineReadState & 0x20) == 0;
            }
        }

        private bool IsDPadBeingRead
        {
            get
            {
                return (LineReadState & 0x10) == 0;
            }
        }

        public byte Read(int position)
        {
            if (position == 0xFF00)
            {
                return Joy1;
            }
            else
            {
                return 0xFF;
            }
        }

        public void Write(int position, byte data)
        {
            if (position == 0xFF00)
                Joy1 = data;
        }

        public void UpdateInput(KeyState ks)
        {
            int diff1 = 
                (keyState.buttonState ^ ks.buttonState) & ks.buttonState | 
                ((keyState.dpadState ^ ks.dpadState) & ks.dpadState);
            if (diff1 != 0)
            {
                interruptManager.RequestInterrupt(InterruptType.Joypad);
            }
            keyState.buttonState = ks.buttonState;
            keyState.dpadState = ks.dpadState;
        }
    }
}