using System;
using System.Reflection;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class OffsetAdd: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void OffsetAddedEventHandler();
        public event OffsetAddedEventHandler OffsetAdded;

        private bool _always;
        public bool AlwaysShowing {
            get => _always;
            set {
                if (value != _always) {
                    _always = value;
                    this.Get<Grid>("Root").MinWidth = _always? 22 : 0;
                }
            }
        }

        public OffsetAdd() => InitializeComponent();

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) OffsetAdded?.Invoke();
        }
    }
}
