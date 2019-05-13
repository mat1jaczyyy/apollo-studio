using System;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Apollo.Components {
    public class HorizontalAdd: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void AddedEventHandler();
        public event AddedEventHandler Added;

        public delegate void ActionEventHandler(string action);
        public event ActionEventHandler Action;

        Grid Root;
        Canvas Icon;

        ContextMenu ActionContextMenu;

        private bool _always;
        public bool AlwaysShowing {
            get => _always;
            set {
                if (value != _always) {
                    _always = value;
                    Root.MinWidth = _always? 26 : 0;
                }
            }
        }

        public HorizontalAdd() {
            InitializeComponent();
            
            Root = this.Get<Grid>("Root");
            Icon = this.Get<Canvas>("Icon");

            ActionContextMenu = (ContextMenu)this.Resources["ActionContextMenu"];
            ActionContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(ActionContextMenu_Click));
        }

        private void ActionContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Action?.Invoke((string)((MenuItem)item).Header);
        }

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) Added?.Invoke();
            else if (e.MouseButton == MouseButton.Right) ActionContextMenu.Open(Icon);
            e.Handled = true;
        }
    }
}
