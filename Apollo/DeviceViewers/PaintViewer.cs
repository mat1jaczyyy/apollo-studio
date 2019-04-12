using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;
using Apollo.Structures;

namespace Apollo.DeviceViewers {
    public class PaintViewer: UserControl {
        public static readonly string DeviceIdentifier = "paint";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Paint _paint;
        ColorPicker Picker;

        public PaintViewer(Paint paint) {
            InitializeComponent();

            _paint = paint;

            Picker = this.Get<ColorPicker>("Picker");
            Picker.SetColor(_paint.Color);
        }
        
        private void Color_Changed(Color color) => _paint.Color = color;
    }
}
