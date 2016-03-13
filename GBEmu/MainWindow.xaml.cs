using GBEmu.Emulator;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace GBEmu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GBSystem gbs;
        private Thread gbSysThread;
        private ThreadStart sysStart;
        private WPFRenderWindow renderWindow;

        public MainWindow()
        {
            InitializeComponent();
            renderWindow = mainRenderWindow;
            renderWindow.InitializeWindow(160, 144);
            gbs = new GBSystem(renderWindow, new Win32InputHandler(), new HighResTimer());
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            gbs.Stop();
            if (gbSysThread != null)
            {
                gbSysThread.Abort();
            }
            base.OnClosing(e);
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
            gbSysThread.SetApartmentState(ApartmentState.STA);
            gbSysThread.Start();
            mainRenderWindow.Focus();
        }

        #endregion System Control

        #region Menu Items

        private void openToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == true)
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

        private void exitToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void showLogToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new LogWindow().ShowDialog();
        }

        #endregion Menu Items

        private void Window_Activated(object sender, EventArgs e)
        {
            if (gbs != null)
                gbs.Focus();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (gbs != null)
                gbs.Unfocus();
        }

        private void showVRAMToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new TilemapWindow(renderWindow).Show();
        }

        private void showMonitorToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new MonitorForm().Show();
        }
    }
}