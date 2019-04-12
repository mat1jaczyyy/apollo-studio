using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using Apollo.Core;

namespace Apollo.Components {
    public class SaveButton: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public SaveButton() => InitializeComponent();

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) Program.Project.Save((Window)this.GetVisualRoot());
        }
    }
}
