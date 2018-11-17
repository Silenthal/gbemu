using GBEmu.Emulator.Debug;
using System.Windows;

namespace GBEmu
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        public LogWindow()
        {
            InitializeComponent();
            var s = Logger.GetInstance().GetMessages();
            for (int i = 0; i < s.Count; i++)
            {
                logBox.Items.Add(s[i]);
            }
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void clearMsgButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.GetInstance().ClearMessages();
            logBox.Items.Clear();
        }
    }
}