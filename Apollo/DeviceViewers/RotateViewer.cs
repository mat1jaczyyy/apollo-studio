using System;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class RotateViewer: UserControl {
        public static readonly string DeviceIdentifier = "rotate";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Rotate _rotate;
        ComboBox RotateMode;
        CheckBox Bypass;

        public RotateViewer(Rotate rotate) {
            InitializeComponent();

            _rotate = rotate;

            RotateMode = this.Get<ComboBox>("RotateMode");
            RotateMode.SelectedItem = _rotate.Mode;
            
            Bypass = this.Get<CheckBox>("Bypass");
            Bypass.IsChecked = _rotate.Bypass;
        }

        private void Mode_Changed(object sender, SelectionChangedEventArgs e) => _rotate.Mode = (string)RotateMode.SelectedItem;

        private void Bypass_Changed(object sender, EventArgs e) => _rotate.Bypass = Bypass.IsChecked.Value;
    }
}
