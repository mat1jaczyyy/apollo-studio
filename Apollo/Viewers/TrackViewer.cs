using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Elements;
using Apollo.Windows;

namespace Apollo.Viewers {
    public class TrackViewer: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        Track _track;
        
        public TrackViewer(Track track) {
            InitializeComponent();
            
            _track = track;
            
            this.Get<TextBlock>("Name").Text = $"Track {_track.ParentIndex + 1}";
        }
        
        private void Clicked(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) TrackWindow.Create(_track);
        }
    }
}
