namespace GBEmu
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Windows.Forms;
    using GBEmu.Emulator;
    using SharpDX.Multimedia;

    public partial class MainForm : Form
    {
        private GBSystem gbs;
        private Thread gbSysThread;
        private ThreadStart sysStart;
        private WPFRenderWindow renderWindow;

        public MainForm()
        {
            InitializeComponent();
            renderWindow = (WPFRenderWindow)elementHost1.Child;
            renderWindow.InitializeWindow(160, 144);
            gbs = new GBSystem(renderWindow, new Win32InputHandler(), new HighResTimer());
        }

        protected override void WndProc(ref Message m)
        {
            switch(m.Msg)
            {
                case 0x219:// WM_DEVICECHANGE
                    {
                        // TODO: Handle controller hook/unhook
                        break;
                    }
            }
            base.WndProc(ref m);
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
            elementHost1.Focus();
        }

        #endregion System Control

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
                gbs.LoadFile(openFileDialog1.FileName);
                StartSystem();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void showLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new LogWindow().ShowDialog();
        }

        #endregion Menu Items

        private void MainForm_Activated(object sender, EventArgs e)
        {
            if (gbs != null)
                gbs.Focus();
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            if (gbs != null)
                gbs.Unfocus();
        }

        private void showVRAMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new TilemapWindow(renderWindow).Show();
        }

        private void showMonitorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new MonitorForm().Show();
        }
    }
}