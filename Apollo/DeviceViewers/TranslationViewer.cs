using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class TranslationViewer: UserControl {
        public static readonly string DeviceIdentifier = "translation";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Translation _translation;

        public TranslationViewer(Translation translation) {
            InitializeComponent();

            _translation = translation;
            this.Get<Dial>("Offset").RawValue = _translation.Offset;
        }

        private void Offset_Changed(double value) {
            _translation.Offset = (int)value;
        }
    }
}
