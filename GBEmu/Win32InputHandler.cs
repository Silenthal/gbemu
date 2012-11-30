namespace GBEmu
{
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using GBEmu.Emulator;
    using GBEmu.Emulator.Input;

    public struct KeySettings
    {
        public Keys Button_A;
        public Keys Button_B;
        public Keys Button_Start;
        public Keys Button_Select;
        public Keys Button_Up;
        public Keys Button_Down;
        public Keys Button_Left;
        public Keys Button_Right;
        public Keys Button_Reset;
        public Keys Button_Pause;
        public Keys Button_ToggleFrameLimit;
    }

    public class Win32InputHandler : IInputHandler
    {
        private KeySettings keySettings;

        public Win32InputHandler()
        {
            keySettings = new KeySettings();
            keySettings.Button_A = Keys.X;
            keySettings.Button_B = Keys.Z;
            keySettings.Button_Start = Keys.Enter;
            keySettings.Button_Select = Keys.ShiftKey;
            keySettings.Button_Up = Keys.Up;
            keySettings.Button_Down = Keys.Down;
            keySettings.Button_Left = Keys.Left;
            keySettings.Button_Right = Keys.Right;
            keySettings.Button_Pause = Keys.P;
            keySettings.Button_ToggleFrameLimit = Keys.F;
        }

        public void PollInput(GBSystem system)
        {
            system.KeyChange(GBKeys.A, IsKeyDown(keySettings.Button_A));
            system.KeyChange(GBKeys.B, IsKeyDown(keySettings.Button_B));
            system.KeyChange(GBKeys.Start, IsKeyDown(keySettings.Button_Start));
            system.KeyChange(GBKeys.Select, IsKeyDown(keySettings.Button_Select));
            system.KeyChange(GBKeys.Up, IsKeyDown(keySettings.Button_Up));
            system.KeyChange(GBKeys.Down, IsKeyDown(keySettings.Button_Down));
            system.KeyChange(GBKeys.Left, IsKeyDown(keySettings.Button_Left));
            system.KeyChange(GBKeys.Right, IsKeyDown(keySettings.Button_Right));
            if (IsKeyToggled(keySettings.Button_Pause))
            {
                if (system.state == GBSystemState.Running)
                    system.Pause();
                else if (system.state == GBSystemState.Paused)
                    system.Resume();
            }
            if (IsKeyToggled(keySettings.Button_ToggleFrameLimit))
            {
                system.ToggleFrameSpeed();
            }
        }

        public bool IsKeyDown(Keys vKey)
        {
            return (NativeMethods.GetAsyncKeyState((int)vKey) & 0x8000) != 0;
        }

        public bool IsKeyToggled(Keys vKey)
        {
            return (NativeMethods.GetAsyncKeyState((int)vKey) & 0x1) != 0;
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern short GetAsyncKeyState(int vKey);

            [DllImport("user32.dll")]
            public static extern short GetKeyState(int vKey);
        }
    }
}