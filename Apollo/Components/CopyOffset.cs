using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Devices;
using Apollo.Structures;

namespace Apollo.Components {
    public class CopyOffset: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void OffsetEventHandler(int index);
        public event OffsetEventHandler OffsetAdded;
        public event OffsetEventHandler OffsetRemoved;
        
        Offset _offset;
        Copy _copy;
        MoveDial _viewer;

        public CopyOffset(Offset offset, Copy copy) {
            InitializeComponent();

            _offset = offset;
            _copy = copy;

            _viewer = this.Get<MoveDial>("Offset");
            _viewer.X = _offset.X;
            _viewer.Y = _offset.Y;
            _viewer.Changed += Offset_Changed;
        }

        private void Offset_Changed(int x, int y) {
            _offset.X = x;
            _offset.Y = y;
        }

        private void Offset_Add() => OffsetAdded?.Invoke(_copy.Offsets.IndexOf(_offset) + 1);

        private void Offset_Remove() => OffsetRemoved?.Invoke(_copy.Offsets.IndexOf(_offset));
    }
}
