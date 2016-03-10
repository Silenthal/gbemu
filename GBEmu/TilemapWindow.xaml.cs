using System.ComponentModel;
using System.Windows;

namespace GBEmu
{
    /// <summary>
    /// Interaction logic for TilemapWindow.xaml
    /// </summary>
    public partial class TilemapWindow : Window
    {
        public WPFRenderWindow renderWindow;
        private WPFRenderWindow baseWindow;

        public TilemapWindow(WPFRenderWindow render)
        {
            InitializeComponent();
            renderWindow = elementHost1;
            renderWindow.InitializeWindow(128, 192);
            baseWindow = render;
            baseWindow.RegisterTilemap(renderWindow);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            baseWindow.UnregisterTilemap();
        }
    }
}