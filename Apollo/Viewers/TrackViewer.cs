using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Elements;
using Apollo.Windows;

namespace Apollo.Viewers {
    public class TrackViewer: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void TrackAddedEventHandler(int index);
        public event TrackAddedEventHandler TrackAdded;
        
        Track _track;

        public void UpdateText(int index) {
            this.Get<TextBlock>("Name").Text = $"Track {index + 1}";
        }
        
        public TrackViewer(Track track) {
            InitializeComponent();
            
            _track = track;
            
            UpdateText(_track.ParentIndex.Value);
            _track.ParentIndexChanged += UpdateText;
        }
        
        private void Clicked(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) TrackWindow.Create(_track);
        }

        private void Track_Add() {
            TrackAdded?.Invoke(_track.ParentIndex.Value + 1);
        }
    }
}
