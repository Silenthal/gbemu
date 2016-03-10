using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace GBEmu
{
    /// <summary>
    /// Interaction logic for MonitorWindow.xaml
    /// </summary>
    public partial class MonitorForm : Window
    {
        private Thread updateThread;

        public MonitorForm()
        {
            InitializeComponent();
            updateThread = new Thread(new ThreadStart(() =>
            {
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
            Dispatcher.CurrentDispatcher.BeginInvoke(new UpdateBarDelegate((value) =>
            {
                mainBar.Value = (int)(val > 100 ? 100 : val);
                mainBarLabel.Content = "CPU: " + val.ToString(".000") + "%";
            }));
        }

        public void UpdateBlit(double val)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(new UpdateBarDelegate((value) =>
            {
                blitBar.Value = (int)(val > 100 ? 100 : val);
                blitBarLabel.Content = "Blit: " + val.ToString(".000") + "%";
            }));
        }
    }
}