using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

using Apollo.Core;
using Apollo.Windows;

namespace Apollo.Components {
    public class UndoButton: IconButton {
        void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        void Update_Position(int position) => Enabled = position != 0;

        protected override IBrush Fill {
            get => (IBrush)this.Resources["Brush"];
            set => this.Resources["Brush"] = value;
        }

        public UndoButton() {
            InitializeComponent();

            AllowRightClick = true;
            AllowRightClickEvenIfDisabled = true;
            base.MouseLeave(this, null);

            Program.Project.Undo.PositionChanged += Update_Position;
            Update_Position(Program.Project.Undo.Position);
        }

        protected override void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            base.Unloaded(sender, e);

            if (Program.Project.Undo != null)
                Program.Project.Undo.PositionChanged -= Update_Position;
        }

        protected override void Click(PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonReleased) Program.Project.Undo.Undo();
            else if (MouseButton == PointerUpdateKind.RightButtonReleased) UndoWindow.Create((Window)this.GetVisualRoot());
        }
    }
}
