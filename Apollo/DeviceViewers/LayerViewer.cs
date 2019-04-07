using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class LayerViewer: UserControl {
        public static readonly string DeviceIdentifier = "layer";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Layer _layer;

        public LayerViewer(Layer layer) {
            InitializeComponent();

            _layer = layer;
            this.Get<Dial>("Target").RawValue = _layer.Target;
        }

        private void Target_Changed(double value) => _layer.Target = (int)value;
    }
}
