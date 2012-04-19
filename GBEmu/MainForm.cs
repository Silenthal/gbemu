using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using GBEmu.Emulator;
using GBEmu.EmuTiming.Win32;
using GBEmu.Input.Win32;

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
			gbs = new GBSystem(xnaRenderWindow1, new Win32InputHandler(), new HighResTimer());
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

		#region System Control
		private void StartSystem()
		{
			if (gbSysThread != null)
			{
				gbSysThread.Abort();
			}
			sysStart = new ThreadStart(gbs.StartSystem);
			gbSysThread = new Thread(sysStart);
			gbSysThread.Start();
			xnaRenderWindow1.Focus();
		}

		private void PauseSystem()
		{
			gbs.Pause();
		}

		private void ResumeSystem()
		{
			gbs.Resume();
			xnaRenderWindow1.Focus();
		}
		#endregion

		#region Menu Items
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
				StartSystem();
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}
		#endregion

		private void MainForm_Activated(object sender, EventArgs e)
		{
			if (gbs != null) gbs.Focus();
		}

		private void MainForm_Deactivate(object sender, EventArgs e)
		{
			if (gbs != null) gbs.Unfocus();
		}
	}
}
