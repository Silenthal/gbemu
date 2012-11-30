namespace GBEmu.Emulator
{
    using GBEmu.Emulator.Audio;
    using GBEmu.Emulator.Cartridge;
    using GBEmu.Emulator.Debug;
    using GBEmu.Emulator.Graphics;
    using GBEmu.Emulator.Input;
    using GBEmu.Emulator.IO;
    using GBEmu.Emulator.Timing;
    using GBEmu.Render;

    public enum GBSystemState
    {
        Stopped,
        Running,
        Paused
    }

    public enum FocusStatus
    {
        Unfocused,
        Focused
    }

    public class GBSystem
    {
        #region Emulation Components

        private IRenderable screen;
        private IInputHandler inputHandler;
        private ITimekeeper watch;

        public GBSystemState state
        {
            get;
            private set;
        }

        public FocusStatus focusStatus
        {
            get;
            private set;
        }

        #endregion Emulation Components

        #region Emulation Speed Settings

        private static double framesPerSecondDMG = (double)4194304 / (double)70224;
        private static double frameTimeDMG = 1 / framesPerSecondDMG;

        private static double[] SpeedLimits =
		{
			frameTimeDMG * 2.0,	//Half
			frameTimeDMG,		//Normal
			frameTimeDMG * 0.5,	//Double
			0					//Limited By Screen Refresh
		};

        private int frameLimitIndex;

        #endregion Emulation Speed Settings

        #region GB System Components

        private InterruptManager interruptManager;
        private Video video;
        private GBAudio audio;
        private GBTimer timer;
        private Cart cart;
        private GBInput input;
        private Serial serial;
        private CPU cpu;
        private MMU mmu;

        #endregion GB System Components

        public bool FileLoaded
        {
            get;
            private set;
        }

        public GBSystem(IRenderable renderWindow, IInputHandler iInputHandler, ITimekeeper timeKeeper)
        {
            screen = renderWindow;
            state = GBSystemState.Stopped;
            focusStatus = FocusStatus.Focused;
            inputHandler = iInputHandler;
            watch = timeKeeper;
            frameLimitIndex = 1;
        }

        public void LoadFile(byte[] loadFile)
        {
            interruptManager = new InterruptManager();
            timer = new GBTimer(interruptManager);
            serial = new Serial();
            audio = new GBAudio();
            video = new Video(interruptManager, screen);
            cart = CartLoader.LoadCart(loadFile);
            input = new GBInput(interruptManager);
            mmu = new MMU(interruptManager, cart, input, audio, timer, serial, video);
            cpu = new CPU(interruptManager, mmu.Read, mmu.Write, mmu.UpdateTime);
        }

        public void LoadFile(string path, bool loadSaveIfPresent = true)
        {
            LoadFile(FileIO.LoadFile(path));
            var saveName = FileIO.GetPathWithDifferentExtension(path, ".sav");
            if (FileIO.Exists(saveName))
            {
                LoadCartRAM(FileIO.LoadFile(saveName));
            }
        }

        public byte[] SaveCartRAM()
        {
            return cart.SaveOutsideRAM();
        }

        public void LoadCartRAM(byte[] newRAM)
        {
            cart.LoadOutsideRAM(newRAM);
        }

        public uint[] getTilemap()
        {
            return video.WriteTileMap();
        }

        public void StartSystem()
        {
            state = GBSystemState.Running;
            while (state != GBSystemState.Stopped)
            {
                if (focusStatus == FocusStatus.Focused)
                {
                    inputHandler.PollInput(this);
                }
                if (state == GBSystemState.Paused)
                    continue;
                watch.Start();
                cpu.RunFor(video.TimeToNextScreenBlit());
                screen.BlitScreen();
                while (watch.ElapsedTime() < SpeedLimits[frameLimitIndex])
                {
                }
            }
        }

        public void KeyChange(GBKeys key, bool isDown)
        {
            input.KeyChange(key, isDown);
        }

        #region Emulation Control

        public void Stop()
        {
            state = GBSystemState.Stopped;
        }

        public void Resume()
        {
            state = GBSystemState.Running;
        }

        internal void Pause()
        {
            state = GBSystemState.Paused;
        }

        public void Unfocus()
        {
            focusStatus = FocusStatus.Unfocused;
        }

        public void Focus()
        {
            focusStatus = FocusStatus.Focused;
        }

        public void ToggleFrameSpeed()
        {
            frameLimitIndex++;
            if (frameLimitIndex >= SpeedLimits.Length)
            {
                frameLimitIndex = 0;
            }
        }

        #endregion Emulation Control
    }
}