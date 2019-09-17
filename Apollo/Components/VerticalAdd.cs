using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Apollo.Components {
    public class VerticalAdd: AddButton {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Root = this.Get<Grid>("Root");
            Path = this.Get<Path>("Path");
            Icon = this.Get<Canvas>("Icon");
        }
        
        public delegate void ActionEventHandler(string action);
        public event ActionEventHandler Action;

        Canvas Icon;

        public enum AvailableActions {
            None, Paste, PasteAndImport
        }

        AvailableActions _actions = AvailableActions.None;
        public AvailableActions Actions {
            get => _actions;
            set {
                if (value != _actions) {
                    _actions = value;

                    if (_actions == AvailableActions.None) ActionContextMenu = null;
                    else if (_actions == AvailableActions.Paste) ActionContextMenu = (ContextMenu)this.Resources["PasteContextMenu"];
                    else if (_actions == AvailableActions.PasteAndImport) ActionContextMenu = ((ContextMenu)this.Resources["PasteAndImportContextMenu"]);
                }
            }
        }

        ContextMenu ActionContextMenu = null;

        public override bool AlwaysShowing {
            set {
                if (value != _always) {
                    _always = value;
                    Root.MinHeight = _always? 26 : 0;
                }
            }
        }
        
        public VerticalAdd() {
            InitializeComponent();

            AllowRightClick = true;
            base.MouseLeave(this, null);

            ((ContextMenu)this.Resources["PasteContextMenu"]).AddHandler(MenuItem.ClickEvent, ActionContextMenu_Click);
            ((ContextMenu)this.Resources["PasteAndImportContextMenu"]).AddHandler(MenuItem.ClickEvent, ActionContextMenu_Click);
        }

        protected override void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            ((ContextMenu)this.Resources["PasteContextMenu"]).RemoveHandler(MenuItem.ClickEvent, ActionContextMenu_Click);
            ((ContextMenu)this.Resources["PasteAndImportContextMenu"]).RemoveHandler(MenuItem.ClickEvent, ActionContextMenu_Click);
            ActionContextMenu = null;
            
            Action = null;
            base.Unloaded(sender, e);
        }

        void ActionContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Action?.Invoke((string)((MenuItem)item).Header);
        }

        protected override void Click(PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetPointerPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonReleased) InvokeAdded();
            else if (MouseButton == PointerUpdateKind.RightButtonReleased) ActionContextMenu?.Open(Icon);
        }
    }
}
