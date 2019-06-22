using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.DeviceViewers {
    public class MoveViewer: UserControl {
        public static readonly string DeviceIdentifier = "move";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Move _move;

        MoveDial Offset;
        ComboBox GridMode;
        CheckBox Wrap;

        public MoveViewer(Move move) {
            InitializeComponent();

            _move = move;

            Offset = this.Get<MoveDial>("Offset");
            Offset.X = _move.Offset.X;
            Offset.Y = _move.Offset.Y;
            Offset.Changed += Offset_Changed;

            GridMode = this.Get<ComboBox>("GridMode");
            GridMode.SelectedItem = _move.GridMode;

            Wrap = this.Get<CheckBox>("Wrap");
            Wrap.IsChecked = _move.Wrap;
        }

        private void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            Offset.Changed -= Offset_Changed;

            _move = null;
        }

        private void Offset_Changed(int x, int y, int? old_x, int? old_y) {
            _move.Offset.X = x;
            _move.Offset.Y = y;

            if (old_x != null && old_y != null) {
                int ux = old_x.Value;
                int uy = old_y.Value;
                int rx = x;
                int ry = y;

                List<int> path = Track.GetPath(_move);

                Program.Project.Undo.Add($"Move Offset Changed to {rx},{ry}", () => {
                    Move move = ((Move)Track.TraversePath(path));
                    move.Offset.X = ux;
                    move.Offset.Y = uy;

                }, () => {
                    Move move = ((Move)Track.TraversePath(path));
                    move.Offset.X = rx;
                    move.Offset.Y = ry;
                });
            }
        }

        public void SetOffset(int x, int y) {
            Offset.X = x;
            Offset.Y = y;
        }
        
        private void GridMode_Changed(object sender, SelectionChangedEventArgs e) {
            string selected = (string)GridMode.SelectedItem;

            if (_move.GridMode != selected) {
                string u = _move.GridMode;
                string r = selected;
                List<int> path = Track.GetPath(_move);

                Program.Project.Undo.Add($"Move Grid Changed to {selected}", () => {
                    ((Move)Track.TraversePath(path)).GridMode = u;
                }, () => {
                    ((Move)Track.TraversePath(path)).GridMode = r;
                });

                _move.GridMode = selected;
            }
        }

        public void SetGridMode(string mode) => GridMode.SelectedItem = mode;

        private void Wrap_Changed(object sender, EventArgs e) {
            bool value = Wrap.IsChecked.Value;

            if (_move.Wrap != value) {
                bool u = _move.Wrap;
                bool r = value;
                List<int> path = Track.GetPath(_move);

                Program.Project.Undo.Add($"Move Wrap Changed to {(r? "Enabled" : "Disabled")}", () => {
                    ((Move)Track.TraversePath(path)).Wrap = u;
                }, () => {
                    ((Move)Track.TraversePath(path)).Wrap = r;
                });

                _move.Wrap = value;
            }
        }

        public void SetWrap(bool value) => Wrap.IsChecked = value;
    }
}
