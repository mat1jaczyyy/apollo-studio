using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;

namespace Apollo.DeviceViewers {
    public class MoveViewer: UserControl {
        public static readonly string DeviceIdentifier = "move";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Offset = this.Get<MoveDial>("Offset");
            GridMode = this.Get<ComboBox>("GridMode");
            Wrap = this.Get<CheckBox>("Wrap");
        }
        
        Move _move;

        MoveDial Offset;
        ComboBox GridMode;
        CheckBox Wrap;

        public MoveViewer() => new InvalidOperationException();

        public MoveViewer(Move move) {
            InitializeComponent();

            _move = move;

            SetOffset(_move.Offset);
            Offset.Changed += Offset_Changed;
            Offset.AbsoluteChanged += Offset_AbsoluteChanged;
            Offset.Switched += Offset_Switched;

            GridMode.SelectedIndex = (int)_move.GridMode;

            Wrap.IsChecked = _move.Wrap;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            Offset.Changed -= Offset_Changed;
            Offset.AbsoluteChanged -= Offset_AbsoluteChanged;
            Offset.Switched -= Offset_Switched;

            _move = null;
        }

        void Offset_Changed(int x, int y, int? old_x, int? old_y) {
            _move.Offset.X = x;
            _move.Offset.Y = y;

            if (old_x != null && old_y != null) {
                int ux = old_x.Value;
                int uy = old_y.Value;
                int rx = x;
                int ry = y;

                List<int> path = Track.GetPath(_move);

                Program.Project.Undo.Add($"Move Offset Relative Changed to {rx},{ry}", () => {
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

        void Offset_AbsoluteChanged(int x, int y, int? old_x, int? old_y) {
            _move.Offset.AbsoluteX = x;
            _move.Offset.AbsoluteY = y;

            if (old_x != null && old_y != null) {
                int ux = old_x.Value;
                int uy = old_y.Value;
                int rx = x;
                int ry = y;

                List<int> path = Track.GetPath(_move);

                Program.Project.Undo.Add($"Move Offset Absolute Changed to {rx},{ry}", () => {
                    Move move = ((Move)Track.TraversePath(path));
                    move.Offset.AbsoluteX = ux;
                    move.Offset.AbsoluteY = uy;

                }, () => {
                    Move move = ((Move)Track.TraversePath(path));
                    move.Offset.AbsoluteX = rx;
                    move.Offset.AbsoluteY = ry;
                });
            }
        }

        void Offset_Switched() {
            bool u = _move.Offset.IsAbsolute;
            bool r = !_move.Offset.IsAbsolute;

            List<int> path = Track.GetPath(_move);

            Program.Project.Undo.Add($"Move Offset Switched to {(r? "Absolute" : "Relative")}", () => {
                Move move = ((Move)Track.TraversePath(path));
                move.Offset.IsAbsolute = u;

            }, () => {
                Move move = ((Move)Track.TraversePath(path));
                move.Offset.IsAbsolute = r;
            });

            _move.Offset.IsAbsolute = !_move.Offset.IsAbsolute;
        }

        public void SetOffset(Offset offset) {
            Offset.Update(offset);
            Wrap.IsEnabled = !offset.IsAbsolute;
        }
        
        void GridMode_Changed(object sender, SelectionChangedEventArgs e) {
            GridType selected = (GridType)GridMode.SelectedIndex;

            if (_move.GridMode != selected) {
                GridType u = _move.GridMode;
                GridType r = selected;
                List<int> path = Track.GetPath(_move);

                Program.Project.Undo.Add($"Move Grid Changed to {((ComboBoxItem)GridMode.ItemContainerGenerator.ContainerFromIndex((int)r)).Content}", () => {
                    ((Move)Track.TraversePath(path)).GridMode = u;
                }, () => {
                    ((Move)Track.TraversePath(path)).GridMode = r;
                });

                _move.GridMode = selected;
            }
        }

        public void SetGridMode(GridType mode) => GridMode.SelectedIndex = (int)mode;

        void Wrap_Changed(object sender, RoutedEventArgs e) {
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
