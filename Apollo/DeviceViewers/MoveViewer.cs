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

        ComboBox ComboBox;
        CheckBox Loop;

        public MoveViewer(Move move) {
            InitializeComponent();

            _move = move;

            MoveDial moveDial = this.Get<MoveDial>("Offset");
            moveDial.X = _move.Offset.X;
            moveDial.Y = _move.Offset.Y;
            moveDial.Changed += Offset_Changed;

            ComboBox = this.Get<ComboBox>("ComboBox");
            ComboBox.SelectedItem = _move.GridMode;

            Loop = this.Get<CheckBox>("Loop");
            Loop.IsChecked = _move.Loop;
        }

        private void Offset_Changed(int x, int y) {
            _move.Offset.X = x;
            _move.Offset.Y = y;
        }

        
        private void GridMode_Changed(object sender, SelectionChangedEventArgs e) => _move.GridMode = (string)ComboBox.SelectedItem;

        private void Loop_Changed(object sender, EventArgs e) => _move.Loop = Loop.IsChecked.Value;
    }
}
