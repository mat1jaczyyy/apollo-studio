using System;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class RotateViewer: UserControl {
        public static readonly string DeviceIdentifier = "rotate";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Rotate _rotate;
        ComboBox ComboBox;
        CheckBox Bypass;

        public RotateViewer(Rotate rotate) {
            InitializeComponent();

            _rotate = rotate;

            ComboBox = this.Get<ComboBox>("ComboBox");
            ComboBox.SelectedItem = _rotate.Mode;
            Mode_Changed(null, null);
            
            Bypass = this.Get<CheckBox>("Bypass");
            Bypass.IsChecked = _rotate.Bypass;
            Bypass_Changed(null, EventArgs.Empty);
        }

        private void Mode_Changed(object sender, SelectionChangedEventArgs e) => _rotate.Mode = (string)ComboBox.SelectedItem;

        private void Bypass_Changed(object sender, EventArgs e) => _rotate.Bypass = Bypass.IsChecked.Value;
    }
}
