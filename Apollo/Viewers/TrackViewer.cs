using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
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
        
        private void Clicked(object sender, EventArgs e) {
            if (_track.Window == null) {
                new TrackWindow(_track).Show();
            } else {
                _track.Window.Activate();
            }
        }
    }
}
