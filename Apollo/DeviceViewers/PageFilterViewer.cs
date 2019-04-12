using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Input;

using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class PageFilterViewer: UserControl {
        public static readonly string DeviceIdentifier = "pagefilter";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        PageFilter _filter;
        UniformGrid PagesGrid;

        private void Set(Rectangle rect, bool value) => rect.Fill = (IBrush)Application.Current.Styles.FindResource(value? "ThemeExtraBrush" : "ThemeForegroundLowBrush");

        public PageFilterViewer(PageFilter filter) {
            InitializeComponent();

            _filter = filter;

            PagesGrid = this.Get<UniformGrid>("PagesGrid");

            for (int i = 0; i < PagesGrid.Children.Count; i++)
                Set((Rectangle)PagesGrid.Children[i], _filter[i]);
        }

        private void Clicked(object sender, PointerReleasedEventArgs e) {
            int index = PagesGrid.Children.IndexOf((IControl)sender);
            Set((Rectangle)sender, _filter[index] = !_filter[index]);
        }
    }
}
