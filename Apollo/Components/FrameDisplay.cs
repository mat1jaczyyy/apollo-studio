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
        public event FrameEventHandler FrameSelected;
        
        Pattern _pattern;
        public FrameThumbnail Viewer;
        public Remove Remove;
        public VerticalAdd FrameAdd;

        public FrameDisplay(Frame frame, Pattern pattern) {
            InitializeComponent();

            _pattern = pattern;

            Viewer = this.Get<FrameThumbnail>("Frame");
            Viewer.Frame = frame;
            
            Remove = this.Get<Remove>("Remove");
            FrameAdd = this.Get<VerticalAdd>("FrameAdd");
        }

        private void Frame_Add() => FrameAdded?.Invoke(_pattern.Frames.IndexOf(Viewer.Frame) + 1);

        private void Frame_Remove() => FrameRemoved?.Invoke(_pattern.Frames.IndexOf(Viewer.Frame));

        private void Clicked() => FrameSelected?.Invoke(_pattern.Frames.IndexOf(Viewer.Frame));
    }
}
