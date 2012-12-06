using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Windows.Threading;

namespace GBEmu
{
    public partial class MonitorForm : Form
    {
        private Thread updateThread;

        public MonitorForm()
        {
            InitializeComponent();
            updateThread = new Thread(new ThreadStart(() => {
                HighResTimer hrt = new HighResTimer();
                while (true)
                {
                    hrt.Start();
                    UpdateMain(GBMonitor.CPUTime);
                    UpdateBlit(GBMonitor.BlitTime);
                }
            }));
            updateThread.Start();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            updateThread.Abort();
            base.OnClosing(e);
        }

        public delegate void UpdateBarDelegate(double val);

        public void UpdateMain(double val)
        {
            if (mainBar.InvokeRequired)
            {
                mainBar.Invoke(new UpdateBarDelegate(UpdateMain), val);
            }
            else
            {
                mainBar.Value = (int)(val > 100 ? 100 : val);
                mainBarLabel.Text = "CPU: " + val.ToString(".000") + "%";
            }
        }

        public void UpdateBlit(double val)
        {
            if (blitBar.InvokeRequired)
            {
                blitBar.Invoke(new UpdateBarDelegate(UpdateBlit), val);
            }
            else
            {
                blitBar.Value = (int)(val > 100 ? 100 : val);
                blitBarLabel.Text = "Blit: " + val.ToString(".000") + "%";
            }
        }
    }
}
