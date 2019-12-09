using System;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Apollo.Components {
    public class ApolloContextMenu: ContextMenu, IStyleable {
        Type IStyleable.StyleKey => typeof(ContextMenu);

        public delegate void MenuActionEventHandler(string action);
        public event MenuActionEventHandler MenuAction;

        public delegate void MenuActionWithItemEventHandler(MenuItem item, string action);
        public event MenuActionWithItemEventHandler MenuActionWithItem;

        public delegate void MenuActionWithSenderEventHandler(ApolloContextMenu sender, string action);
        public event MenuActionWithSenderEventHandler MenuActionWithSender;

        public ApolloContextMenu() {
            AvaloniaXamlLoader.Load(this);

            this.AddHandler(MenuItem.ClickEvent, Selected);
        }

        string header;

        void Selected(object sender, RoutedEventArgs e) {
            if (e.Source is MenuItem menuItem) {
                header = (string)menuItem.Header;

                MenuAction?.Invoke(header);
                MenuActionWithItem?.Invoke(menuItem, header);
                MenuActionWithSender?.Invoke(this, header);
            }
        }

        Window owner;

        void Closed(object sender, RoutedEventArgs e) {
            if (header != "Rename") owner?.Focus();
        }

        public new void Open(Control control) {
            owner = (Window)control.GetVisualRoot();

            base.Open(control);
        }
    }
}