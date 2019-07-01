using System;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Apollo.Components {
    public class DeviceAdd: AddButton {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Root = this.Get<Grid>("Root");
            Path = this.Get<Path>("Path");
            Icon = this.Get<Canvas>("Icon");
        }

        public new delegate void AddedEventHandler(Type device);
        public new event AddedEventHandler Added;

        public delegate void ActionEventHandler(string action);
        public event ActionEventHandler Action;

        Canvas Icon;

        ContextMenu AddContextMenu, DeviceContextMenu;

        public override bool AlwaysShowing {
            set {
                if (value != _always) {
                    _always = value;
                    Root.MinWidth = _always? 30 : 0;
                }
            }
        }

        public DeviceAdd() {
            InitializeComponent();

            AllowRightClick = true;
            base.MouseLeave(this, null);

            AddContextMenu = (ContextMenu)this.Resources["AddContextMenu"];
            AddContextMenu.AddHandler(MenuItem.ClickEvent, AddContextMenu_Click);

            DeviceContextMenu = (ContextMenu)this.Resources["DeviceContextMenu"];
            DeviceContextMenu.AddHandler(MenuItem.ClickEvent,  DeviceContextMenu_Click);
        }

        protected override void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            Added = null;
            Action = null;

            AddContextMenu.RemoveHandler(MenuItem.ClickEvent, AddContextMenu_Click);
            DeviceContextMenu.RemoveHandler(MenuItem.ClickEvent, DeviceContextMenu_Click);

            AddContextMenu = DeviceContextMenu = null;

            base.Unloaded(sender, e);
        }

        void AddContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Added?.Invoke(Assembly.GetExecutingAssembly().GetType($"Apollo.Devices.{((string)((MenuItem)item).Header).Replace(" ", "")}"));
        }

        void DeviceContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Action?.Invoke((string)((MenuItem)item).Header);
        }

        protected override void Click(PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) AddContextMenu.Open(Icon);
            else if (e.MouseButton == MouseButton.Right) DeviceContextMenu.Open(Icon);
        }
    }
}
