using System;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class FlipViewer: UserControl {
        public static readonly string DeviceIdentifier = "flip";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Flip _flip;
        ComboBox FlipMode;
        CheckBox Bypass;

        public FlipViewer(Flip flip) {
            InitializeComponent();

            _flip = flip;

            FlipMode = this.Get<ComboBox>("FlipMode");
            FlipMode.SelectedItem = _flip.Mode;
            
            Bypass = this.Get<CheckBox>("Bypass");
            Bypass.IsChecked = _flip.Bypass;
        }

        private void Mode_Changed(object sender, SelectionChangedEventArgs e) => _flip.Mode = (string)FlipMode.SelectedItem;

        private void Bypass_Changed(object sender, EventArgs e) => _flip.Bypass = Bypass.IsChecked.Value;
    }
}
