using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class PaintViewer: UserControl {
        public static readonly string DeviceIdentifier = "paint";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Paint _paint;
        Ellipse color;

        public PaintViewer(Paint paint) {
            InitializeComponent();

            _paint = paint;
            
            color = this.Get<Ellipse>("Color");
            color.Fill = _paint.Color.ToBrush();
        }

        private void MainThumbMove(object sender, VectorEventArgs e) {
            Thumb thumb = (Thumb)e.Source;
            Canvas Area = (Canvas)thumb.Parent;

            double x = Canvas.GetLeft(thumb) + e.Vector.X;
            double y = Canvas.GetTop(thumb) + e.Vector.Y;

            if (0 <= x && x <= Area.Bounds.Width) Canvas.SetLeft(thumb, x);
            if (0 <= y && y <= Area.Bounds.Height) Canvas.SetTop(thumb, y);
        }

        private void HueThumbMove(object sender, VectorEventArgs e) {
            Thumb thumb = (Thumb)e.Source;
            Canvas Area = (Canvas)thumb.Parent;

            double x = Canvas.GetLeft(thumb) + e.Vector.X;
            if (0 <= x && x <= Area.Bounds.Width) Canvas.SetLeft(thumb, x);
        }
    }
}
