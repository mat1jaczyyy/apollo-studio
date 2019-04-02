using System;
using System.Reflection;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class ChainAdd: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void ChainAddedEventHandler();
        public event ChainAddedEventHandler ChainAdded;
        
        public ChainAdd() => InitializeComponent();

        private void Clicked(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) ChainAdded?.Invoke();
        }
    }
}
