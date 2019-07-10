using System;

using Avalonia;
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
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Path = this.Get<Path>("Path");
        }

        ContextMenu SaveContextMenu;

        void Update_Saved(bool saved) => Enabled = !saved;

        Path Path;

        protected override IBrush Fill {
            get => Path.Fill;
            set => Path.Fill = value;
        }

        public SaveButton() {
            InitializeComponent();

            AllowRightClick = true;
            base.MouseLeave(this, null);

            SaveContextMenu = (ContextMenu)this.Resources["SaveContextMenu"];
            SaveContextMenu.AddHandler(MenuItem.ClickEvent, SaveContextMenu_Click);

            Program.Project.Undo.SavedChanged += Update_Saved;
            Update_Saved(Program.Project.Undo.Saved);
        }

        protected override void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            base.Unloaded(sender, e);
            
            if (Program.Project.Undo != null)
                Program.Project.Undo.SavedChanged -= Update_Saved;
            
            SaveContextMenu.RemoveHandler(MenuItem.ClickEvent, SaveContextMenu_Click);
            SaveContextMenu = null;
        }

        async void SaveContextMenu_Click(object sender, EventArgs e) {
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
