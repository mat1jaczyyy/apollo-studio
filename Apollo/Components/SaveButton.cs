using System;

using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

using Apollo.Core;

namespace Apollo.Components {
    public class SaveButton: IconButton {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        ContextMenu SaveContextMenu;

        private void Update_Saved(bool saved) => Enabled = !saved;

        Path Path;

        protected override IBrush Fill {
            get => Path.Fill;
            set => Path.Fill = value;
        }

        public SaveButton() {
            InitializeComponent();

            Path = this.Get<Path>("Path");

            AllowRightClick = true;
            base.MouseLeave(this, null);

            SaveContextMenu = (ContextMenu)this.Resources["SaveContextMenu"];
            SaveContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(SaveContextMenu_Click));

            Program.Project.Undo.SavedChanged += Update_Saved;
            Update_Saved(Program.Project.Undo.Saved);
        }

        private async void SaveContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem)) {
                switch (((MenuItem)item).Header) {
                    case "Save as...":
                        await Program.Project.Save((Window)this.GetVisualRoot(), true);
                        break;

                    case "Save a copy...":
                        await Program.Project.Save((Window)this.GetVisualRoot(), false);
                        break;
                }
            }
        }

        protected override async void Click(PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) await Program.Project.Save((Window)this.GetVisualRoot());
            else if (e.MouseButton == MouseButton.Right) SaveContextMenu.Open(this);
        }
    }
}
