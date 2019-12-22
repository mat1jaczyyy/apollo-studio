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

            Program.Project.Undo.SavedChanged += Update_Saved;
            Update_Saved(Program.Project.Undo.Saved);
        }

        protected override void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            base.Unloaded(sender, e);
            
            if (Program.Project.Undo != null)
                Program.Project.Undo.SavedChanged -= Update_Saved;
        }

        async void ContextMenu_Action(string action) => await Program.Project.Save((Window)this.GetVisualRoot(), action == "Save as...");

        protected override async void Click(PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;
            
            if (MouseButton == PointerUpdateKind.LeftButtonReleased) await Program.Project.Save((Window)this.GetVisualRoot());
            else if (MouseButton == PointerUpdateKind.RightButtonReleased) ((ApolloContextMenu)this.Resources["SaveContextMenu"]).Open(this);
        }
    }
}
