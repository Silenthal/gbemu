using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using GBEmu.Emulator;

namespace GBEmu
{
	public struct KeyPack
	{
		public Keys Button_A;
		public Keys Button_B;
		public Keys Button_Start;
		public Keys Button_Select;
		public Keys Button_Up;
		public Keys Button_Down;
		public Keys Button_Left;
		public Keys Button_Right;
	}

	public partial class MainForm : Form
	{
		GBSystem gbs;
		bool fileLoaded = false;
		Thread gbSysThread;
		ThreadStart sysStart;
		KeyPack keySettings;

		public MainForm()
		{
			InitializeComponent();
			gbs = new GBSystem(xnaRenderWindow1);

			keySettings = new KeyPack();
			keySettings.Button_A = Keys.X;
			keySettings.Button_B = Keys.Z;
			keySettings.Button_Start = Keys.Enter;
			keySettings.Button_Select = Keys.ShiftKey;
			keySettings.Button_Up = Keys.Up;
			keySettings.Button_Down = Keys.Down;
			keySettings.Button_Left = Keys.Left;
			keySettings.Button_Right = Keys.Right;
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
						xnaRenderWindow1.Focus();
						break;
					case GBSystemState.Paused:
						gbs.Resume();
						xnaRenderWindow1.Focus();
						break;
					case GBSystemState.Running:
						gbs.Pause();
						TextBoxWrite(gbs.FetchCPUState());
						break;
				}
			}
		}

		delegate void TextWrite(string info);

		private void TextBoxWrite(string info)
		{
			if (richTextBox1.InvokeRequired)
			{
				TextWrite tw = new TextWrite(TextBoxWrite);
				richTextBox1.Invoke(tw, new object[] { info });
			}
			else
			{
				richTextBox1.Text = info;
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

		private void MainForm_KeyDown(object sender, KeyEventArgs e)
		{
			if (gbs.state == GBSystemState.Running)
			{
				if (e.KeyCode == keySettings.Button_A)
				{
					gbs.KeyChange(GBKeys.A, true);
				}
				if (e.KeyCode == keySettings.Button_B)
				{
					gbs.KeyChange(GBKeys.B, true);
				}
				if (e.KeyCode == keySettings.Button_Start)
				{
					gbs.KeyChange(GBKeys.Start, true);
				}
				if (e.KeyCode == keySettings.Button_Select)
				{
					gbs.KeyChange(GBKeys.Select, true);
				}
				if (e.KeyCode == keySettings.Button_Up)
				{
					gbs.KeyChange(GBKeys.Up, true);
				}
				if (e.KeyCode == keySettings.Button_Down)
				{
					gbs.KeyChange(GBKeys.Down, true);
				}
				if (e.KeyCode == keySettings.Button_Left)
				{
					gbs.KeyChange(GBKeys.Left, true);
				}
				if (e.KeyCode == keySettings.Button_Right)
				{
					gbs.KeyChange(GBKeys.Right, true);
				}
			}
			e.Handled = true;
			e.SuppressKeyPress = true;
		}

		private void MainForm_KeyUp(object sender, KeyEventArgs e)
		{
			if (gbs.state == GBSystemState.Running)
			{
				if (gbs.state == GBSystemState.Running)
				{
					if (e.KeyCode == keySettings.Button_A)
					{
						gbs.KeyChange(GBKeys.A, false);
					}
					if (e.KeyCode == keySettings.Button_B)
					{
						gbs.KeyChange(GBKeys.B, false);
					}
					if (e.KeyCode == keySettings.Button_Start)
					{
						gbs.KeyChange(GBKeys.Start, false);
					}
					if (e.KeyCode == keySettings.Button_Select)
					{
						gbs.KeyChange(GBKeys.Select, false);
					}
					if (e.KeyCode == keySettings.Button_Up)
					{
						gbs.KeyChange(GBKeys.Up, false);
					}
					if (e.KeyCode == keySettings.Button_Down)
					{
						gbs.KeyChange(GBKeys.Down, false);
					}
					if (e.KeyCode == keySettings.Button_Left)
					{
						gbs.KeyChange(GBKeys.Left, false);
					}
					if (e.KeyCode == keySettings.Button_Right)
					{
						gbs.KeyChange(GBKeys.Right, false);
					}
				}
			}
			e.Handled = true;
			e.SuppressKeyPress = true;
		}

		private void MainForm_KeyPress(object sender, KeyPressEventArgs e)
		{
			e.Handled = true;
		}
	}
}
