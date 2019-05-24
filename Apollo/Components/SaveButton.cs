using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

using Apollo.Core;

namespace Apollo.Components {
    public class SaveButton: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        ContextMenu SaveContextMenu;

        private void Update_Saved(bool saved) =>
            this.Resources["CanvasBrush"] = (SolidColorBrush)Application.Current.Styles.FindResource(saved
                ? "ThemeControlHighBrush"
                : "ThemeForegroundLowBrush"
            );

        public SaveButton() {
            InitializeComponent();

            SaveContextMenu = (ContextMenu)this.Resources["SaveContextMenu"];
            SaveContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(SaveContextMenu_Click));

            Program.Project.Undo.SavedChanged += Update_Saved;
            Update_Saved(Program.Project.Undo.Saved);
        }

        private void SaveContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem)) {
                switch (((MenuItem)item).Header) {
                    case "Save as...":
                        Program.Project.Save((Window)this.GetVisualRoot(), true);
                        break;

                    case "Save a copy...":
                        Program.Project.Save((Window)this.GetVisualRoot(), false);
                        break;
                }
            }
        }

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) Program.Project.Save((Window)this.GetVisualRoot());
            else if (e.MouseButton == MouseButton.Right) SaveContextMenu.Open(this);
        }
    }
}
