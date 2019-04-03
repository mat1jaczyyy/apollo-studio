using System;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Input;

using Apollo.Components;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class FilterViewer: UserControl {
        public static readonly string DeviceIdentifier = "filter";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Filter _filter;

        public FilterViewer(Filter filter) {
            InitializeComponent();

            _filter = filter;
        }

        private void Clicked(object sender, PointerReleasedEventArgs e) {
            
        }
    }
}
