using System;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Apollo.Components {
    public class DeviceAdd: UserControl {
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Root = this.Get<Grid>("Root");
            Icon = this.Get<Canvas>("Icon");

            AddContextMenu = (ContextMenu)this.Resources["AddContextMenu"];
            DeviceContextMenu = (ContextMenu)this.Resources["DeviceContextMenu"];
        }

        public delegate void DeviceAddedEventHandler(Type device);
        public event DeviceAddedEventHandler DeviceAdded;

        public delegate void DeviceActionEventHandler(string action);
        public event DeviceActionEventHandler DeviceAction;

        Grid Root;
        Canvas Icon;

        ContextMenu AddContextMenu, DeviceContextMenu;

        private bool _always;
        public bool AlwaysShowing {
            get => _always;
            set {
                if (value != _always) {
                    _always = value;
                    Root.MinWidth = _always? 30 : 0;
                }
            }
        }

        public DeviceAdd() {
            InitializeComponent();
            
            AddContextMenu.AddHandler(MenuItem.ClickEvent, AddContextMenu_Click);
            DeviceContextMenu.AddHandler(MenuItem.ClickEvent,  DeviceContextMenu_Click);
        }

        private void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            DeviceAdded = null;
            DeviceAction = null;

            AddContextMenu.RemoveHandler(MenuItem.ClickEvent, AddContextMenu_Click);
            DeviceContextMenu.RemoveHandler(MenuItem.ClickEvent, DeviceContextMenu_Click);

            AddContextMenu = DeviceContextMenu = null;
        }

        private void AddContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                DeviceAdded?.Invoke(Assembly.GetExecutingAssembly().GetType($"Apollo.Devices.{(string)((MenuItem)item).Header}"));
        }

        private void DeviceContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
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
