using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Apollo.Components;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class KeyFilterViewer: UserControl {
        public static readonly string DeviceIdentifier = "keyfilter";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        KeyFilter _filter;
        LaunchpadGrid Grid;

        private SolidColorBrush GetColor(bool value) => (SolidColorBrush)Application.Current.Styles.FindResource(value? "ThemeAccentBrush" : "ThemeForegroundLowBrush");

        public KeyFilterViewer(KeyFilter filter) {
            InitializeComponent();

            _filter = filter;

            Grid = this.Get<LaunchpadGrid>("Grid");

            for (int i = 0; i < 100; i++)
                Grid.SetColor(LaunchpadGrid.SignalToGrid(i), GetColor(_filter[i]));
        }

        bool drawingState;
        
        private void PadStarted(int index) => drawingState = !_filter[LaunchpadGrid.GridToSignal(index)];
        private void PadPressed(int index) => Grid.SetColor(index, GetColor(_filter[LaunchpadGrid.GridToSignal(index)] = drawingState));
    }
}
