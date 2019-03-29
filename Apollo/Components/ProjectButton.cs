using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using Apollo.Windows;

namespace Apollo.Components {
    public class ProjectButton: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public ProjectButton() => InitializeComponent();

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) ProjectWindow.Create((Window)this.GetVisualRoot());
        }
    }
}
