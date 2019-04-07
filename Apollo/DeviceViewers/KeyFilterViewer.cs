using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Input;

using Apollo.Components;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class KeyFilterViewer: UserControl {
        public static readonly string DeviceIdentifier = "keyfilter";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        KeyFilter _filter;
        UniformGrid LaunchpadGrid;

        private int GridToSignal(int index) => (index == -1)? 99 : ((9 - (index / 10)) * 10 + index % 10);

        private void Set(Shape shape, bool value) => shape.Fill = (IBrush)Application.Current.Styles.FindResource(value? "ThemeAccentBrush" : "ThemeForegroundLowBrush");

        public KeyFilterViewer(KeyFilter filter) {
            InitializeComponent();

            _filter = filter;

            LaunchpadGrid = this.Get<UniformGrid>("LaunchpadGrid");

            for (int i = 0; i < LaunchpadGrid.Children.Count; i++) {
                int index = GridToSignal(i);
                if (index != 0 && index != 9 && index != 90 && index != 99) Set((Shape)LaunchpadGrid.Children[i], _filter[index]);
            }
        }

        private void Clicked(object sender, PointerReleasedEventArgs e) {
            int index = GridToSignal(LaunchpadGrid.Children.IndexOf((IControl)sender));
            Set((Shape)sender, _filter[index] = !_filter[index]);
        }
    }
}
