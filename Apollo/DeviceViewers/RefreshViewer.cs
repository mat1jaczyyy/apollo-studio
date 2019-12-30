using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Devices;
using Apollo.Elements;
using Apollo.Windows;

namespace Apollo.DeviceViewers {
    public class RefreshViewer: UserControl {
        public static readonly string DeviceIdentifier = "refresh";

        void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Refresh _refresh;

        public RefreshViewer() => new InvalidOperationException();

        public RefreshViewer(Refresh refresh) {
            InitializeComponent();

            _refresh = refresh;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _refresh = null;
    }
}
