using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using GBEmu.Emulator;
using GBEmu.Render.Gdi;

namespace GBEmu
{

    public partial class MainForm : Form
    {
        Bitmap bt = new Bitmap(160, 144);
        
        Stopwatch sw = new Stopwatch();
        GBSystem gbs;
        bool fileLoaded = false;
        Thread gbSysThread;
        TimeSpan frame = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / (long)59.7275005);
        bool WorkDone = false;
        
        

        public MainForm()
        {
            InitializeComponent();
            gbs = new GBSystem(gdiWindow1);
            gbSysThread = new Thread(gbs.DoWork);
            sw = new Stopwatch();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            gbSysThread.Abort();
            base.OnFormClosing(e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (fileLoaded)
            {
                if (gbs.Run)
                {
                    gbs.Stop();
                }
                else
                {
                    gbs.Start();
                    new Thread(RunSystem).Start();
                }
            }
        }

        private void RunSystem()
        {
            sw.Start();
            while (gbs.Run)
            {
                sw.Restart();
                gbs.RunSingleFrame();
                while (sw.Elapsed < frame) { }
                ScreenBlit(gbs.newFrame);
            }
            sw.Reset();
            TextBlit(gbs.ExecutedFrames.ToString());
            UpdateLabel(afLabel, gbs.cpu.AF.w.ToString("X4"));
            UpdateLabel(bcLabel, gbs.cpu.BC.w.ToString("X4"));
            UpdateLabel(deLabel, gbs.cpu.DE.w.ToString("X4"));
            UpdateLabel(hlLabel, gbs.cpu.HL.w.ToString("X4"));
            UpdateLabel(spLabel, gbs.cpu.SP.w.ToString("X4"));
            UpdateLabel(pcLabel, gbs.cpu.PC.w.ToString("X4"));
        }

        delegate void ScreenMethod(byte[] frame);
        delegate void TextWrite(string info);
        delegate void labelWrite(Label l, string text);

        private void ScreenBlit(byte[] frame)
        {
            if (gdiWindow1.InvokeRequired)
            {
                ScreenMethod dt = new ScreenMethod(ScreenBlit);
                gdiWindow1.Invoke(dt, new object[] { frame });
            }
            else
            {
                gdiWindow1.CopyImageData(frame);
                gdiWindow1.Invalidate();
            }
        }
        private void TextBlit(string info)
        {
            if (richTextBox1.InvokeRequired)
            {
                TextWrite tw = new TextWrite(TextBlit);
                richTextBox1.Invoke(tw, new object[] { info });
            }
            else
            {
                richTextBox1.AppendText(info);
            }
        }
        private void UpdateLabel(Label l, string info)
        {
            if (l.InvokeRequired)
            {
                labelWrite lw = new labelWrite(UpdateLabel);
                l.Invoke(lw, new object[] { l, info });
            }
            else
            {
                l.Text = info;
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (gbs.Run)
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
            if (gbs.Run)
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
