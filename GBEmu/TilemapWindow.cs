namespace GBEmu
{
    using System.ComponentModel;
    using System.Threading;
    using System.Windows.Forms;
    using System.Windows.Threading;

    public partial class TilemapWindow : Form
    {
        public WPFRenderWindow renderWindow;
        private WPFRenderWindow baseWindow;

        public TilemapWindow(WPFRenderWindow render)
        {
            InitializeComponent();
            
            renderWindow = (WPFRenderWindow)elementHost1.Child;
            renderWindow.InitializeWindow(128, 192);
            baseWindow = render;
            baseWindow.RegisterTilemap(renderWindow);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            baseWindow.UnregisterTilemap();
        }
    }
}