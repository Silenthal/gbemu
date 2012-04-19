using GBEmu.EmuTiming;
using GBEmu.EmuTiming.Win32;
using GBEmu.Input;
using GBEmu.Input.Win32;
using GBEmu.Render;
using GBEmu.Emulator.Cartridge;

namespace GBEmu.Emulator
{
	public enum GBSystemState { Stopped, Running, Paused }

	public enum FocusStatus { Unfocused, Focused }

	class GBSystem
	{
		#region Emulation Components
		private IRenderable screen;
		private IInputHandler inputHandler;
		private ITimekeeper watch;
		public GBSystemState state { get; private set; }
		public FocusStatus focusStatus { get; private set; }
		#endregion
		
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
		#endregion

		#region GB System Components
		private InterruptManager interruptManager;
		private Video video;
		private Audio audio;
		private GBTimer timer;
		private Cart cart;
		private Input input;
		private Serial serial;
		private CPU cpu;
		private MMU mmu;
		#endregion

		public bool FileLoaded { get; private set; }

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
			audio = new Audio();
			video = new Video(interruptManager, screen);
			cart = CartLoader.LoadCart(loadFile);
			input = new Input(interruptManager);
			mmu = new MMU(cart, input, interruptManager, screen, timer, serial, audio, video, video.OAMDMAWrite);
			cpu = new CPU(interruptManager, mmu.Read, mmu.Write, mmu.UpdateCounter);
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
				if (state == GBSystemState.Paused) continue;
				watch.Start();
				cpu.RunFor(70224 - video.ExecutedFrameCycles);
				screen.BlitScreen();
				while (watch.ElapsedTime() < SpeedLimits[frameLimitIndex]) { }
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
		#endregion
	}
}
