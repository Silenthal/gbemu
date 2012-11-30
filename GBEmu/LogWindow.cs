namespace GBEmu
{
    using System;
    using System.Windows.Forms;
    using GBEmu.Emulator.Debug;

    public partial class LogWindow : Form
    {
        public LogWindow()
        {
            InitializeComponent();
            var s = Logger.GetInstance().GetMessages();
            foreach (LogMessage st in s)
            {
                logBox.Items.Add(st);
            }
        }

        private void clearMsgButton_Click(object sender, EventArgs e)
        {
            Logger.GetInstance().ClearMessages();
            logBox.Items.Clear();
        }
    }
}