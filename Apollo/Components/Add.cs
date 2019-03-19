using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class Add: UserControl {
        public static List<string> devices = new List<string>() {
            "Delay"
        };

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public Add() {
            InitializeComponent();

            this.Get<Canvas>("AddCanvas").ContextMenu = new ContextMenu() {
                Items = devices
            };
        }
    }
}
