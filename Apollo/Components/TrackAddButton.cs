using System;
using System.Reflection;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class TrackAddButton: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void TrackAddedEventHandler();
        public event TrackAddedEventHandler TrackAdded;
        
        public TrackAddButton() {
            InitializeComponent();
        }

        private void Clicked(object sender, PointerReleasedEventArgs e) {
            TrackAdded?.Invoke();
        }
    }
}
