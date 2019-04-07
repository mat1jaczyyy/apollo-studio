using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class PageSwitchViewer: UserControl {
        public static readonly string DeviceIdentifier = "pageswitch";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        PageSwitch _pageswitch;

        public PageSwitchViewer(PageSwitch pageswitch) {
            InitializeComponent();

            _pageswitch = pageswitch;
            this.Get<Dial>("Target").RawValue = _pageswitch.Target;
        }

        private void Target_Changed(double value) => _pageswitch.Target = (int)value;
    }
}
