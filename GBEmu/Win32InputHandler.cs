namespace GBEmu
{
    using GBEmu.Emulator.Input;
    using SharpDX.DirectInput;
    using System;
    using System.Runtime.InteropServices;
    using Keys = System.Windows.Input.Key;

    public struct KeySettings
    {
        public Keys Keyboard_Button_A;
        public Keys Keyboard_Button_B;
        public Keys Keyboard_Button_Start;
        public Keys Keyboard_Button_Select;
        public Keys Keyboard_Button_Up;
        public Keys Keyboard_Button_Down;
        public Keys Keyboard_Button_Left;
        public Keys Keyboard_Button_Right;
        public Keys Keyboard_Button_Reset;
        public Keys Keyboard_Button_Pause;
        public Keys Keyboard_Button_FrameLimit;
        public int Controller_Button_A;
        public int Controller_Button_B;
        public int Controller_Button_Start;
        public int Controller_Button_Select;
    }

    public class Win32InputHandler : IInputHandler
    {
        private KeySettings keySettings;
        private Guid controllerGuid;
        private DirectInput input;
        private Joystick controller;
        private HighResTimer pollTimer;
        private JoystickState defaultControllerState;

        public Win32InputHandler()
        {
            pollTimer = new HighResTimer();
            keySettings = new KeySettings();
            keySettings.Keyboard_Button_A = Keys.X;
            keySettings.Keyboard_Button_B = Keys.Z;
            keySettings.Keyboard_Button_Start = Keys.Return;
            keySettings.Keyboard_Button_Select = Keys.RightShift;
            keySettings.Keyboard_Button_Up = Keys.Up;
            keySettings.Keyboard_Button_Down = Keys.Down;
            keySettings.Keyboard_Button_Left = Keys.Left;
            keySettings.Keyboard_Button_Right = Keys.Right;
            keySettings.Keyboard_Button_Pause = Keys.P;
            keySettings.Keyboard_Button_FrameLimit = Keys.F;
            keySettings.Controller_Button_A = 1;
            keySettings.Controller_Button_B = 2;
            keySettings.Controller_Button_Start = 9;
            keySettings.Controller_Button_Select = 8;
            input = new DirectInput();
            InitializeController(Guid.Empty);
        }

        public void InitializeController(Guid initGuid)
        {
            controllerGuid = Guid.Empty;
            var deviceInst = input.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
            if (deviceInst.Count == 0)
            {
                deviceInst = input.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AttachedOnly);
            }
            if (deviceInst.Count > 0)
            {
                foreach (var device in deviceInst)
                {
                    if (device.InstanceGuid == initGuid)
                    {
                        controllerGuid = initGuid;
                    }
                }
                if (controllerGuid == Guid.Empty)
                {
                    controllerGuid = deviceInst[0].InstanceGuid;
                }
                controller = new Joystick(input, controllerGuid);
                controller.Acquire();
                defaultControllerState = controller.GetCurrentState();
            }
        }

        public KeyState PollInput()
        {
            KeyState ret = new KeyState();
            ret.IsADown = IsKeyDown(keySettings.Keyboard_Button_A);
            ret.IsBDown = IsKeyDown(keySettings.Keyboard_Button_B);
            ret.IsStartDown = IsKeyDown(keySettings.Keyboard_Button_Start);
            ret.IsSelectDown = IsKeyDown(keySettings.Keyboard_Button_Select);
            ret.IsUpDown = IsKeyDown(keySettings.Keyboard_Button_Up);
            ret.IsDownDown = IsKeyDown(keySettings.Keyboard_Button_Down);
            ret.IsLeftDown = IsKeyDown(keySettings.Keyboard_Button_Left);
            ret.IsRightDown = IsKeyDown(keySettings.Keyboard_Button_Right);
            ret.IsPauseToggled = IsKeyToggled(keySettings.Keyboard_Button_Pause);
            ret.IsFrameLimitToggled = IsKeyToggled(keySettings.Keyboard_Button_FrameLimit);
            if (controller != null)
            {
                var keyState = controller.GetCurrentState();
                ret.IsADown |= keyState.Buttons[keySettings.Controller_Button_A];
                ret.IsBDown |= keyState.Buttons[keySettings.Controller_Button_B];
                ret.IsStartDown |= keyState.Buttons[keySettings.Controller_Button_Start];
                ret.IsSelectDown |= keyState.Buttons[keySettings.Controller_Button_Select];
                ret.IsUpDown |= keyState.Y < defaultControllerState.Y;
                ret.IsDownDown |= keyState.Y > defaultControllerState.Y;
                ret.IsLeftDown |= keyState.X < defaultControllerState.X;
                ret.IsRightDown |= keyState.X > defaultControllerState.X;
            }
            return ret;
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