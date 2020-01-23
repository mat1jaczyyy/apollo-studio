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
            if (old_x != null && old_y != null)
                Program.Project.Undo.AddAndExecute(new Move.OffsetUndoEntry(
                    _move, 
                    old_x.Value, 
                    old_y.Value, 
                    x, 
                    y
                ));
        }

        void Offset_AbsoluteChanged(int x, int y, int? old_x, int? old_y) {
            if (old_x != null && old_y != null)
                Program.Project.Undo.AddAndExecute(new Move.OffsetAbsoluteUndoEntry(
                    _move, 
                    old_x.Value, 
                    old_y.Value, 
                    x, 
                    y
                ));
        }

        void Offset_Switched() => Program.Project.Undo.AddAndExecute(new Move.OffsetSwitchedUndoEntry(
            _move, 
            _move.Offset.IsAbsolute, 
            !_move.Offset.IsAbsolute
        ));

        public void SetOffset(Offset offset) {
            Offset.Update(offset);
            Wrap.IsEnabled = !offset.IsAbsolute;
        }
        
        void GridMode_Changed(object sender, SelectionChangedEventArgs e) {
            GridType selected = (GridType)GridMode.SelectedIndex;

            if (_move.GridMode != selected)
                Program.Project.Undo.AddAndExecute(new Move.GridModeUndoEntry(
                    _move, 
                    _move.GridMode, 
                    selected
                ));
        }

        public void SetGridMode(GridType mode) => GridMode.SelectedIndex = (int)mode;

        void Wrap_Changed(object sender, RoutedEventArgs e) {
            bool value = Wrap.IsChecked.Value;

            if (_move.Wrap != value)
                Program.Project.Undo.AddAndExecute(new Move.WrapUndoEntry(
                    _move, 
                    _move.Wrap, 
                    value
                ));
        }

        public void SetWrap(bool value) => Wrap.IsChecked = value;
    }
}
