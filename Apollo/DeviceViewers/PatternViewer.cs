using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Windows;

namespace Apollo.DeviceViewers {
    public class PatternViewer: UserControl {
        public static readonly string DeviceIdentifier = "pattern";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Pattern _pattern;

        public PatternViewer(Pattern pattern) {
            InitializeComponent();

            _pattern = pattern;
        }

        private void Edit(object sender, RoutedEventArgs e) => PatternWindow.Create(_pattern, Track.Get(_pattern).Window);
    }
}
