using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;

namespace Apollo.Components {
    public abstract class AddButton: UserControl {
        public delegate void AddedEventHandler();
        public event AddedEventHandler Added;

        protected void InvokeAdded() => Added?.Invoke();

        protected Path Path;

        protected IBrush Fill {
            get => Path.Stroke;
            set => Path.Stroke = value;
        }

        protected bool AllowRightClick = false;

        protected Grid Root;

        protected bool _always;
        public virtual bool AlwaysShowing {
            get => _always;
            set {}
        }

        protected virtual void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => Added = null;

        private bool mouseHeld = false;
        private bool mouseOver = false;

        protected void MouseEnter(object sender, PointerEventArgs e) {
            Fill = (IBrush)Application.Current.Styles.FindResource(mouseHeld? "ThemeButtonDownBrush" : "ThemeButtonOverBrush");
            mouseOver = true;
        }

        protected void MouseLeave(object sender, PointerEventArgs e) {
            Fill = (IBrush)Application.Current.Styles.FindResource("ThemeButtonEnabledBrush");
            mouseHeld = mouseOver = false;
        }

        protected void MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton == MouseButton.Left || (AllowRightClick && e.MouseButton == MouseButton.Right)) {
                mouseHeld = true;

                Fill = (IBrush)Application.Current.Styles.FindResource("ThemeButtonDownBrush");
            }
        }

        protected void MouseUp(object sender, PointerReleasedEventArgs e) {
            if (mouseHeld && (e.MouseButton == MouseButton.Left || (AllowRightClick && e.MouseButton == MouseButton.Right))) {
                mouseHeld = false;

                MouseEnter(sender, null);

                Click(e);
            }
        }

        protected virtual void Click(PointerReleasedEventArgs e) => Added?.Invoke();
    }
}
