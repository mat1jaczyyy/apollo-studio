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

        public delegate void DeviceActionEventHandler(string action);
        public event DeviceActionEventHandler DeviceAction;

        Canvas Icon;

        ContextMenu AddContextMenu, DeviceContextMenu;

        private bool _always;
        public bool AlwaysShowing {
            get => _always;
            set {
                if (value != _always) {
                    _always = value;
                    this.Get<Grid>("Root").MinWidth = _always? 30 : 0;
                }
            }
        }

        public DeviceAdd() {
            InitializeComponent();

            Icon = this.Get<Canvas>("Icon");

            AddContextMenu = (ContextMenu)this.Resources["AddContextMenu"];
            DeviceContextMenu = (ContextMenu)this.Resources["DeviceContextMenu"];
            
            AddContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(AddContextMenu_Click));
            DeviceContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(DeviceContextMenu_Click));
        }

        private void AddContextMenu_Click(object sender, EventArgs e) {
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                DeviceAdded?.Invoke(Assembly.GetExecutingAssembly().GetType($"Apollo.Devices.{(string)((MenuItem)item).Header}"));
        }

        private void DeviceContextMenu_Click(object sender, EventArgs e) {
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                DeviceAction?.Invoke((string)((MenuItem)item).Header);
        }

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) AddContextMenu.Open(Icon);
            else if (e.MouseButton == MouseButton.Right) DeviceContextMenu.Open(Icon);
            e.Handled = true;
        }
    }
}
