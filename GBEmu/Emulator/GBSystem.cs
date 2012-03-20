using GBEmu.EmuTiming;
using GBEmu.EmuTiming.Win32;
using GBEmu.Input;
using GBEmu.Input.Win32;
using GBEmu.Render;

namespace GBEmu.Emulator
{
	public enum GBSystemState { Stopped, Running, Paused }

	public enum FocusStatus { Unfocused, Focused }

	class GBSystem
	{
		private static double framesPerSecondDMG = (double)4194304 / (double)70224;
		private static double frameTimeDMG = 1 / framesPerSecondDMG;

		private CPU cpu;
		private Input input;
		private IRenderable screen;
		private IInputHandler handler;
		private ITimekeeper watch;

		public GBSystemState state { get; private set; }
		public FocusStatus focusStatus { get; private set; }
		
		public bool FileLoaded { get; private set; }

		public GBSystem(IRenderable renderWindow)
		{
			screen = renderWindow;
			state = GBSystemState.Stopped;
			focusStatus = FocusStatus.Focused;
			handler = new Win32InputHandler();
			watch = new HighResTimer();
		}

		public void LoadFile(byte[] loadFile)
		{
			cpu = new CPU(loadFile, screen);
			input = cpu.mmu.input;
		}

		public void StartSystem()
		{
			state = GBSystemState.Running;
			while (state != GBSystemState.Stopped)
			{
				if (focusStatus == FocusStatus.Focused)
				{
					handler.PollInput(this);
				}
				if (state == GBSystemState.Paused) continue;
				watch.Start();
				cpu.RunFor(70224 - cpu.mmu.LCD.ExecutedFrameCycles);
				screen.BlitScreen();
				while (watch.ElapsedTime() < frameTimeDMG) { }
			}
		}

		public void KeyChange(GBKeys key, bool isDown)
		{
			input.KeyChange(key, isDown);
		}

		#region System control
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
		#endregion
	}
}
