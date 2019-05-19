using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

using Apollo.Core;

namespace Apollo.Components {
    public class SaveButton: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void Update_Saved(bool saved) =>
            this.Resources["CanvasBrush"] = (SolidColorBrush)Application.Current.Styles.FindResource(saved
                ? "ThemeControlHighBrush"
                : "ThemeForegroundLowBrush"
            );

        public SaveButton() {
            InitializeComponent();

            Program.Project.Undo.SavedChanged += Update_Saved;
            Update_Saved(Program.Project.Undo.Saved);
        }

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) Program.Project.Save((Window)this.GetVisualRoot());
        }
    }
}
