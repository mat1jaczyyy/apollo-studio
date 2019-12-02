using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Apollo.Components {
    public class TrackAdd: AddButton {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Root = this.Get<Grid>("Root");
            Path = this.Get<Path>("Path");
            Icon = this.Get<Canvas>("Icon");
        }

        public delegate void ActionEventHandler(string action);
        public event ActionEventHandler Action;
        
        Canvas Icon;

        ContextMenu ActionContextMenu;

        public override bool AlwaysShowing {
            set {
                if (value != _always) {
                    _always = value;
                    Root.MinHeight = _always? 30 : 0;
                }
            }
        }

        public TrackAdd() {
            InitializeComponent();

            AllowRightClick = true;
            base.MouseLeave(this, null);

            ActionContextMenu = (ContextMenu)this.Resources["ActionContextMenu"];
            ActionContextMenu.AddHandler(MenuItem.ClickEvent, ActionContextMenu_Click);
        }

        protected override void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            ActionContextMenu.RemoveHandler(MenuItem.ClickEvent, ActionContextMenu_Click);
            ActionContextMenu = null;

            Action = null;
            base.Unloaded(sender, e);
        }

        void ActionContextMenu_Click(object sender, RoutedEventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();

            if (e.Source is MenuItem menuItem)
                Action?.Invoke((string)menuItem.Header);
        }

        protected override void Click(PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonReleased) InvokeAdded();
            else if (MouseButton == PointerUpdateKind.RightButtonReleased) ActionContextMenu.Open(Icon);
        }
    }
}
