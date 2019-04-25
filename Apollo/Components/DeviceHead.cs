using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Apollo.Components {
    public class DeviceHead: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public Border Header;

        public DeviceHead(IBrush brush) {
            InitializeComponent();

            Header = this.Get<Border>("Header");
            this.Resources["TitleBrush"] = brush;
        } 
    }
}
