using Avalonia.Controls;
using Avalonia.Controls.Shapes;
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
    }
}
