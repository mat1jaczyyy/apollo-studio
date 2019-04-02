using System;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class MoveViewer: UserControl {
        public static readonly string DeviceIdentifier = "move";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Move _move;
        CheckBox Loop;

        public MoveViewer(Move move) {
            InitializeComponent();

            _move = move;

            MoveDial moveDial = this.Get<MoveDial>("Offset");
            moveDial.X = _move.X;
            moveDial.Y = _move.Y;

            Loop = this.Get<CheckBox>("Loop");
            Loop.IsChecked = _move.Loop;
            Loop_Changed(null, EventArgs.Empty);
        }

        private void Offset_Changed(int x, int y) {
            _move.X = x;
            _move.Y = y;
        }

        private void Loop_Changed(object sender, EventArgs e) => _move.Loop = Loop.IsChecked.Value;
    }
}
