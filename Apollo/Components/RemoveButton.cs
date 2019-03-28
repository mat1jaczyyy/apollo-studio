using System;
using System.Reflection;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class RemoveButton: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void RemovedEventHandler();
        public event RemovedEventHandler Removed;
        
        public RemoveButton() {
            InitializeComponent();
        }

        private void Clicked(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left)
                Removed?.Invoke();
        }
    }
}
