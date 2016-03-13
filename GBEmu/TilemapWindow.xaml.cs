using System.ComponentModel;
using System.Windows;

namespace GBEmu
{
    /// <summary>
    /// Interaction logic for TilemapWindow.xaml
    /// </summary>
    public partial class TilemapWindow : Window
    {
        public WPFRenderWindow _renderWindow;
        private WPFRenderWindow _baseWindow;

        public TilemapWindow(WPFRenderWindow render)
        {
            InitializeComponent();
            _renderWindow = renderWindow;
            _renderWindow.InitializeWindow(128, 192);
            _baseWindow = render;
            _baseWindow.RegisterTilemap(_renderWindow);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _baseWindow.UnregisterTilemap();
        }
    }
}