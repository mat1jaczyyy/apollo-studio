using System;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Core;

namespace Apollo.Components {
    public class DeviceAdd: AddButton {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Root = this.Get<Grid>("Root");
            Path = this.Get<Path>("Path");
            Icon = this.Get<Canvas>("Icon");

            Preferences.CollapseAddDeviceButtonChanged += CollapsePreferenceChanged;
            this.ApplyAlwaysShowing();
        }

        public new delegate void AddedEventHandler(Type device);
        public new event AddedEventHandler Added;

        public delegate void ActionEventHandler(string action);
        public event ActionEventHandler Action;

        Canvas Icon;

        void CollapsePreferenceChanged() => ApplyAlwaysShowing();

        bool requestedAlwaysShowing;

        void ApplyAlwaysShowing() {
            bool forceExpanded = !Preferences.CollapseAddDeviceButton;
            bool effective = forceExpanded || requestedAlwaysShowing;

            if (effective == _always) return;
            _always = effective;
            Root.MinWidth = _always ? 30 : 0;
        }

        public override bool AlwaysShowing {
            set {
                requestedAlwaysShowing = value;
                ApplyAlwaysShowing();
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

            Preferences.CollapseAddDeviceButtonChanged -= CollapsePreferenceChanged;

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
