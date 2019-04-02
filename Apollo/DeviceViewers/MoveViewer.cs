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

            MoveDial moveDial = this.Get<MoveDial>("Offset");
            moveDial.X = _move.X;
            moveDial.Y = _move.Y;
        }

        private void Offset_Changed(int x, int y) {
            _move.X = x;
            _move.Y = y;
        } 
    }
}
