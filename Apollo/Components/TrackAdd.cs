using System;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class TrackAdd: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void TrackAddedEventHandler();
        public event TrackAddedEventHandler TrackAdded;

        public delegate void TrackActionEventHandler(string action);
        public event TrackActionEventHandler TrackAction;
        
        Canvas Icon;

        ContextMenu ActionContextMenu;

        private bool _always;
        public bool AlwaysShowing {
            get => _always;
            set {
                if (value != _always) {
                    _always = value;
                    this.Get<Grid>("Root").MinHeight = _always? 30 : 0;
                }
            }
        }

        public TrackAdd() {
            InitializeComponent();
            
            Icon = this.Get<Canvas>("Icon");

            ActionContextMenu = (ContextMenu)this.Resources["ActionContextMenu"];
            ActionContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(ActionContextMenu_Click));
        }

        private void ActionContextMenu_Click(object sender, EventArgs e) {
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                TrackAction?.Invoke((string)((MenuItem)item).Header);
        }

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) TrackAdded?.Invoke();
            else if (e.MouseButton == MouseButton.Right) ActionContextMenu.Open(Icon);
            e.Handled = true;
        }
    }
}
