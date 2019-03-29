using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class MoveViewer: UserControl {
        public static readonly string DeviceIdentifier = "move";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Move _move;

        public MoveViewer(Move move) {
            InitializeComponent();

            _move = move;
            this.Get<Dial>("Offset").RawValue = _move.Offset;
        }

        private void Offset_Changed(double value) => _move.Offset = (int)value;
    }
}
