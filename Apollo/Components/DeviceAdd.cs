using System;
using System.Reflection;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class DeviceAdd: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void DeviceAddedEventHandler(Type device);
        public event DeviceAddedEventHandler DeviceAdded;

        Canvas Icon;

        private bool _always;
        public bool AlwaysShowing {
            get => _always;
            set {
                if (value != _always) {
                    _always = value;
                    this.Get<Grid>("Root").MinWidth = _always? 26 : 0;
                }
            }
        }

        public DeviceAdd() {
            InitializeComponent();

            Icon = this.Get<Canvas>("Icon");

            Icon.ContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(ContextMenu_Click));
        }

        private void ContextMenu_Click(object _, EventArgs e) {
            IInteractive sender = ((RoutedEventArgs)e).Source;

            if (sender.GetType() == typeof(MenuItem)) {
                string selected = ((MenuItem)sender).Header.ToString();
                DeviceAdded?.Invoke(Assembly.GetExecutingAssembly().GetType($"Apollo.Devices.{selected}"));
            }
        }

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) Icon.ContextMenu.Open(Icon);
            e.Handled = true;
        }
    }
}
