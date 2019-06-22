using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Apollo.Components {
    public class VerticalAdd: UserControl {
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Root = this.Get<Grid>("Root");
            Icon = this.Get<Canvas>("Icon");
        }

        public delegate void AddedEventHandler();
        public event AddedEventHandler Added;
        
        public delegate void ActionEventHandler(string action);
        public event ActionEventHandler Action;

        Grid Root;
        Canvas Icon;

        public enum AvailableActions {
            None, Paste, PasteAndImport
        }

        private AvailableActions _actions = AvailableActions.None;
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

        private bool _always;
        public bool AlwaysShowing {
            get => _always;
            set {
                if (value != _always) {
                    _always = value;
                    Root.MinHeight = _always? 26 : 0;
                }
            }
        }
        
        public VerticalAdd() {
            InitializeComponent();

            ((ContextMenu)this.Resources["PasteContextMenu"]).AddHandler(MenuItem.ClickEvent, ActionContextMenu_Click);
            ((ContextMenu)this.Resources["PasteAndImportContextMenu"]).AddHandler(MenuItem.ClickEvent, ActionContextMenu_Click);
        }

        private void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            Added = null;
            Action = null;

            ((ContextMenu)this.Resources["PasteContextMenu"]).RemoveHandler(MenuItem.ClickEvent, ActionContextMenu_Click);
            ((ContextMenu)this.Resources["PasteAndImportContextMenu"]).RemoveHandler(MenuItem.ClickEvent, ActionContextMenu_Click);
            ActionContextMenu = null;
        }

        private void ActionContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Action?.Invoke((string)((MenuItem)item).Header);
        }

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) Added?.Invoke();
            else if (e.MouseButton == MouseButton.Right) ActionContextMenu?.Open(Icon);
            e.Handled = true;
        }
    }
}
