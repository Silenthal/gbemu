using System;
using System.Diagnostics;
using System.Text;
using GBEmu.Render.Gdi;
using GBEmu.Render;

namespace GBEmu.Emulator
{
	public enum GBSystemState { Stopped, Running, Paused }
	class GBSystem
	{
		static TimeSpan frame = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / (long)59.7275005);
		
		public GBSystemState state;
		
		public CPU cpu;
		public Input input;
		
		Stopwatch stopwatch;
		
		public bool FileLoaded { get; private set; }

		volatile IRenderable screen;
		
		public int ExecutedFrames;

		public GBSystem(IRenderable renderWindow)
		{
			stopwatch = new Stopwatch();
			screen = renderWindow;
			ExecutedFrames = 0;
			state = GBSystemState.Stopped;
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

		public void StartSystem()
		{
			state = GBSystemState.Running;
			stopwatch.Start();
			while (state != GBSystemState.Stopped)
			{
				if (state == GBSystemState.Paused) continue;
				stopwatch.Reset();
				stopwatch.Start();
				cpu.step(70224 - cpu.mmu.LCD.ExecutedFrameCycles);
				ExecutedFrames++;
				while (stopwatch.Elapsed < frame) { }
				screen.RenderFrame();
			}
			stopwatch.Reset();
		}

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

		public string FetchCPUState()
		{
			StringBuilder sb = new StringBuilder();
			lock (cpu)
			{
				sb.AppendLine(cpu.mmu.LCD.ExecutedFrameCycles.ToString());
				sb.AppendLine(cpu.AF.w.ToString("X4"));
				sb.AppendLine(cpu.BC.w.ToString("X4"));
				sb.AppendLine(cpu.DE.w.ToString("X4"));
				sb.AppendLine(cpu.HL.w.ToString("X4"));
				sb.AppendLine(cpu.SP.w.ToString("X4"));
				sb.AppendLine(cpu.PC.w.ToString("X4"));
			}
			return sb.ToString();
		}
	}
}
