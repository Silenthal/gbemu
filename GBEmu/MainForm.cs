using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using GBEmu.Emulator;
using GBEmu.Render.Gdi;
using GBEmu.Render;
using GBEmu.Render.XNA;

namespace GBEmu
{

	public partial class MainForm : Form
	{
		GBSystem gbs;
		bool fileLoaded = false;
		Thread gbSysThread;
		ThreadStart sysStart;

		public MainForm()
		{
			InitializeComponent();
			gbs = new GBSystem(xnaRenderWindow1);
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			gbs.Stop();
			if (gbSysThread != null)
			{
				gbSysThread.Abort();
			}
			base.OnFormClosing(e);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (fileLoaded)
			{
				switch (gbs.state)
				{
					case GBSystemState.Stopped:
						if (gbSysThread != null)
						{
							gbSysThread.Abort();
						}
						sysStart = new ThreadStart(gbs.StartSystem);
						gbSysThread = new Thread(sysStart);
						gbSysThread.Start();
						break;
					case GBSystemState.Paused:
						gbs.Resume();
						break;
					case GBSystemState.Running:
						gbs.Pause();
						TextBlit(gbs.FetchCPUState());
						break;
				}
			}
		}

		delegate void TextWrite(string info);

		private void TextBlit(string info)
		{
			if (richTextBox1.InvokeRequired)
			{
				TextWrite tw = new TextWrite(TextBlit);
				richTextBox1.Invoke(tw, new object[] { info });
			}
			else
			{
				richTextBox1.Text = info;
			}
		}

		private void MainForm_KeyDown(object sender, KeyEventArgs e)
		{
			if (gbs.state == GBSystemState.Running)
			{
				switch(e.KeyCode)
				{
					case Keys.Up:
						lock (gbs) gbs.KeyChange(GBKeys.Up, true);
						break;
					case Keys.Down:
						lock (gbs) gbs.KeyChange(GBKeys.Down, true);
						break;
					case Keys.Left:
						lock (gbs) gbs.KeyChange(GBKeys.Left, true);
						break;
					case Keys.Right:
						lock (gbs) gbs.KeyChange(GBKeys.Right, true);
						break;
					case Keys.A:
						lock (gbs) gbs.KeyChange(GBKeys.A, true);
						break;
					case Keys.B:
						lock (gbs) gbs.KeyChange(GBKeys.B, true);
						break;
					case Keys.Enter:
						lock (gbs) gbs.KeyChange(GBKeys.Start, true);
						break;
					case Keys.RShiftKey:
						lock (gbs) gbs.KeyChange(GBKeys.Select, true);
						break;
					default:
						break;
				}
			}
		}

		private void MainForm_KeyUp(object sender, KeyEventArgs e)
		{
			if (gbs.state == GBSystemState.Running)
			{
				switch (e.KeyCode)
				{
					case Keys.Up:
						gbs.KeyChange(GBKeys.Up, false);
						break;
					case Keys.Down:
						gbs.KeyChange(GBKeys.Down, false);
						break;
					case Keys.Left:
						gbs.KeyChange(GBKeys.Left, false);
						break;
					case Keys.Right:
						gbs.KeyChange(GBKeys.Right, false);
						break;
					case Keys.A:
						gbs.KeyChange(GBKeys.A, false);
						break;
					case Keys.B:
						gbs.KeyChange(GBKeys.B, false);
						break;
					case Keys.Enter:
						gbs.KeyChange(GBKeys.Start, false);
						break;
					case Keys.RShiftKey:
						gbs.KeyChange(GBKeys.Select, false);
						break;
					default:
						break;
				}
			}
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				gbs.Stop();
				if (gbSysThread != null)
				{
					gbSysThread.Abort();
				}
				gbs.LoadFile(File.ReadAllBytes(openFileDialog1.FileName));
				fileLoaded = true;
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}
	}
}
