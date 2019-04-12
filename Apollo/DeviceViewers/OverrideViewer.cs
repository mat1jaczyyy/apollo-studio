using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class OverrideViewer: UserControl {
        public static readonly string DeviceIdentifier = "override";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Override _override;

        public OverrideViewer(Override o) {
            InitializeComponent();
            
            _override = o;
            this.Get<Dial>("Target").RawValue = _override.Target;
        }

        private void Target_Changed(double value) => _override.Target = (int)value - 1;
    }
}
