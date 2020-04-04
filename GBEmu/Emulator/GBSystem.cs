using GBEmu.Emulator.Audio;
using GBEmu.Emulator.Cartridge;
using GBEmu.Emulator.Debug;
using GBEmu.Emulator.Graphics;
using GBEmu.Emulator.Input;
using GBEmu.Emulator.IO;
using GBEmu.Emulator.Memory;
using GBEmu.Emulator.Timing;

namespace GBEmu.Emulator
{
    public class GBSystem
    {
        #region Emulation Components

        private IRenderable screen;
        private IInputHandler inputHandler;
        private ITimekeeper frameTimer;
        private bool isFocused;

        public GBSystemState state
        {
            get;
            private set;
        }

        #endregion Emulation Components

        #region Emulation Speed Settings

        private static double framesPerSecondDMG = (double)4194304 / 70224;
        private static double frameTimeDMG = 1 / framesPerSecondDMG;

        private static double[] SpeedLimits =
        {
            frameTimeDMG * 2.0,	//Half
            frameTimeDMG,		//Normal
            frameTimeDMG * 0.5,	//Double
            0					//Limited By Speed of Emulation
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
        private WRAM wram;
        private HRAM hram;

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
            isFocused = true;
            inputHandler = iInputHandler;
            frameTimer = timeKeeper;
            frameLimitIndex = 1;
        }

        public void LoadFile(byte[] loadFile)
        {
            interruptManager = new InterruptManager();
            timer = new GBTimer(interruptManager);
            serial = new Serial();
            audio = new GBAudio();
            wram = new WRAM();
            hram = new HRAM();
            video = new Video(interruptManager, screen);
            cart = CartLoader.LoadCart(loadFile);
            input = new GBInput(interruptManager, inputHandler);
            mmu = new MMU(interruptManager, cart, input, audio, timer, serial, video, wram, hram);
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
            var frameInput = new KeyState();
            while (state != GBSystemState.Stopped)
            {
                if (frameInput.IsPauseToggled)
                {
                    TogglePause();
                }
                if (frameInput.IsFrameLimitToggled)
                {
                    ToggleFrameSpeed();
                }
                if (state == GBSystemState.Paused)
                {
                    continue;
                }
                Profiler.GetInstance().Restart("Main CPU");
                frameTimer.Start();
                cpu.RunFor(video.TimeToNextVBlank());
                if (isFocused)
                {
                    frameInput = inputHandler.PollInput();
                    input.UpdateInput(frameInput);
                }
                cpu.RunFor(video.TimeToTopOfLCD());
                while (frameTimer.ElapsedSeconds() < SpeedLimits[frameLimitIndex])
                {
                }
                GBMonitor.CPUTime = Profiler.GetInstance().StopAndGetTimeAsFrameTimePercent("Main CPU");
            }
        }

        #region Emulation Control

        public void Stop()
        {
            state = GBSystemState.Stopped;
        }

        public void TogglePause()
        {
            if (state == GBSystemState.Paused)
            {
                state = GBSystemState.Running;
            }
            else if (state == GBSystemState.Running)
            {
                state = GBSystemState.Paused;
            }
        }

        public void Unfocus()
        {
            isFocused = false;
        }

        public void Focus()
        {
            isFocused = true;
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