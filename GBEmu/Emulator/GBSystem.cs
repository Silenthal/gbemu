using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using GBEmu.Render.Gdi;

namespace GBEmu.Emulator
{
	public enum GBSystemState { Stopped, Running, Paused }
	class GBSystem
	{
		public GBSystemState state;
		public CPU cpu;
		Stopwatch stopwatch;
		public bool Run = false;
		TimeSpan frame = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / (long)59.7275005);
		public bool FileLoaded { get; private set; }
		volatile GdiWindow screen;
		public Input input;
		public int ExecutedFrames;
		public byte[] newFrame { get { return cpu.mmu.LCD.LCDMap; } }

		public GBSystem(GdiWindow renderWindow)
		{
			stopwatch = new Stopwatch();
			screen = renderWindow;
			ExecutedFrames = 0;
		}

		public void LoadFile(byte[] loadFile)
		{
			cpu = new CPU(loadFile, screen);
			input = cpu.mmu.input;
		}

		public void KeyChange(GBKeys key, bool isDown)
		{
			input.KeyChange(key, isDown);
		}

		public void DoWork()
		{
			stopwatch.Start();
			while (Run)
			{
				stopwatch.Restart();
				cpu.step(70224 - cpu.mmu.LCD.ExecutedFrameCycles);
				ExecutedFrames++;
				while (stopwatch.Elapsed < frame) { }
				screen.Invalidate();
			}
		}

		public void RunSingleFrame()
		{
			cpu.step(70224 - cpu.mmu.LCD.ExecutedFrameCycles);
			ExecutedFrames++;
		}

		public void Stop()
		{
			Run = false;
		}

		public void Start()
		{
			Run = true;
		}
	}
}
