using System;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

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
        }

        protected override void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            Added = null;
            Action = null;

            base.Unloaded(sender, e);
        }

        void AddContextMenu_Action(string action) => Added?.Invoke(Assembly.GetExecutingAssembly().GetType($"Apollo.Devices.{action.Replace(" ", "")}"));

        void DeviceContextMenu_Action(string action) => Action?.Invoke(action);

        protected override void Click(PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonReleased) ((ApolloContextMenu)this.Resources["AddContextMenu"]).Open(Icon);
            else if (MouseButton == PointerUpdateKind.RightButtonReleased) ((ApolloContextMenu)this.Resources["DeviceContextMenu"]).Open(Icon);
        }
    }
}
