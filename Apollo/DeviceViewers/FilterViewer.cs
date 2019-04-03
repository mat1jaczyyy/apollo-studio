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
    public class FilterViewer: UserControl {
        public static readonly string DeviceIdentifier = "filter";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Filter _filter;
        UniformGrid LaunchpadGrid;

        public FilterViewer(Filter filter) {
            InitializeComponent();

            _filter = filter;

            LaunchpadGrid = this.Get<UniformGrid>("LaunchpadGrid");
        }

        private void Clicked(object sender, PointerReleasedEventArgs e) {
            int index = LaunchpadGrid.Children.IndexOf((IControl)sender);
            int result = (index == -1)? 99 : ((9 - (index / 10)) * 10 + index % 10);

            _filter[result] = !_filter[result];
            ((Shape)sender).Fill = (IBrush)Application.Current.Styles.FindResource(_filter[result]? "ThemeAccentBrush" : "ThemeForegroundLowBrush");
        }
    }
}
