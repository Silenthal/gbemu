﻿namespace GBEmu.Emulator.Graphics
{
    using GBEmu.Emulator.IO;

    /// <summary>
    /// A manager for the LY counter for the display.
    /// </summary>
    internal class LYCounter : TimedIODevice
    {
        public static int LineCycles = 456;

        public byte LY
        {
            get;
            private set;
        }

        public int TimeOnCurrentLine
        {
            get
            {
                return CycleCounter;
            }
        }

        private bool IsInMode2 = false;

        public delegate void OnLineChangedEventHandler();

        public delegate void OnMode3EventHandler();

        public event OnLineChangedEventHandler LineChanged;

        public event OnMode3EventHandler OnMode3;

        protected virtual void OnLineChanged()
        {
            if (LineChanged != null)
                LineChanged();
        }

        protected virtual void OnMode3Reached()
        {
            if (OnMode3 != null)
                OnMode3();
        }

        public override void UpdateTime(int cycles)
        {
            CycleCounter += cycles;
            if (CycleCounter >= LineCycles)
            {
                CycleCounter -= LineCycles;
                IncrementLY();
                if (LY < 144)
                {
                    IsInMode2 = true;
                }
            }
            else if (IsInMode2 && LY < 144 && CycleCounter >= 80)
            {
                IsInMode2 = false;
                OnMode3();
            }
        }

        private void IncrementLY()
        {
            LY++;
            if (LY >= 154)
            {
                LY = 0;
            }
            OnLineChanged();
        }

        public void ResetLY()
        {
            LY = 0;
            CycleCounter = 0;
            OnLineChanged();
            IsInMode2 = true;
        }

        public override byte Read(int position)
        {
            if (position == IOPorts.LY)
            {
                return LY;
            }
            else
                return 0xFF;
        }

        public override void Write(int position, byte data)
        {
        }
    }
}