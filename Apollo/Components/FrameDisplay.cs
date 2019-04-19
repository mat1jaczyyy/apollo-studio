using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Devices;
using Apollo.Structures;

namespace Apollo.Components {
    public class FrameDisplay: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void FrameEventHandler(int index);
        public event FrameEventHandler FrameAdded;
        public event FrameEventHandler FrameRemoved;
        
        Pattern _pattern;
        FrameThumbnail _viewer;

        public FrameDisplay(Frame frame, Pattern pattern) {
            InitializeComponent();

            _pattern = pattern;

            _viewer = this.Get<FrameThumbnail>("Frame");
            _viewer.Frame = frame;
        }

        private void Frame_Add() => FrameAdded?.Invoke(_pattern.Frames.IndexOf(_viewer.Frame) + 1);

        private void Frame_Remove() => FrameRemoved?.Invoke(_pattern.Frames.IndexOf(_viewer.Frame));
    }
}
