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
	class GBSystem
	{
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

		public GBSystem(byte[] loadFile)
		{
			stopwatch = new Stopwatch();
			cpu = new CPU(loadFile);
			input = cpu.mmu.input;
		}

		public void LoadFile(byte[] loadFile)
		{
			cpu = new CPU(loadFile);
		}

		public void KeyChange(GBKeys key, bool isDown)
		{
			switch (key)
			{
				case GBKeys.Up:
				case GBKeys.Down:
				case GBKeys.Left:
				case GBKeys.Right:
					input.KeyChange(key, true, isDown);
					break;
				case GBKeys.A:
				case GBKeys.B:
				case GBKeys.Start:
				case GBKeys.Select:
					input.KeyChange(key, false, isDown);
					break;
				default:
					break;
			}
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
				lock (screen)
				{
					screen.CopyImageData(cpu.mmu.LCD.LCDMap);
					screen.Invalidate();
				}
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
