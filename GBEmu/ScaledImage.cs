namespace GBEmu
{
    using System.Windows.Controls;
    using System.Windows.Media;

    public class ScaledImage : Image
    {
        protected override void OnRender(DrawingContext dc)
        {
            this.VisualBitmapScalingMode = BitmapScalingMode.NearestNeighbor;
            base.OnRender(dc);
        }
    }
}