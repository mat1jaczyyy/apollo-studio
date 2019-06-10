using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Apollo.Components {
    public abstract class IconButton: UserControl {
        public delegate void ClickedEventHandler();
        public event ClickedEventHandler Clicked;

        protected abstract IBrush Fill { get; set; }

        private bool mouseHeld = false;

        protected void MouseEnter(object sender, PointerEventArgs e) {
            Fill = (IBrush)Application.Current.Styles.FindResource(mouseHeld? "ThemeButtonDownBrush" : "ThemeButtonOverBrush");
        }

        protected void MouseLeave(object sender, PointerEventArgs e) {
            Fill = (IBrush)Application.Current.Styles.FindResource("ThemeButtonEnabledBrush");
            mouseHeld = false;
        }

        protected void MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) {
                mouseHeld = true;

                Fill = (IBrush)Application.Current.Styles.FindResource("ThemeButtonDownBrush");
            }
        }

        protected void MouseUp(object sender, PointerReleasedEventArgs e) {
            if (mouseHeld && e.MouseButton == MouseButton.Left) {
                mouseHeld = false;

                MouseEnter(sender, null);
                Click(e);
            }
        }

        protected virtual void Click(PointerReleasedEventArgs e) => Clicked?.Invoke();
    }
}
