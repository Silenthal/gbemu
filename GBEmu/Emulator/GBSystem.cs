using System;
using System.Diagnostics;
using System.Text;
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
				cpu.RunFor(70224 - cpu.mmu.LCD.ExecutedFrameCycles);
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
				sb.AppendLine("Cycles before Next Blit: " + cpu.mmu.LCD.ExecutedFrameCycles.ToString());
				sb.AppendLine("PC = " + cpu.PC.w.ToString("X4"));
				sb.AppendLine("AF = " + cpu.AF.w.ToString("X4"));
				sb.AppendLine("BC = " + cpu.BC.w.ToString("X4"));
				sb.AppendLine("DE = " + cpu.DE.w.ToString("X4"));
				sb.AppendLine("HL = " + cpu.HL.w.ToString("X4"));
				sb.AppendLine("SP = " + cpu.SP.w.ToString("X4"));
			}
			return sb.ToString();
		}
	}
}
