using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Apollo.Components {
    public abstract class IconButton: UserControl {
        public delegate void ClickedEventHandler();
        public event ClickedEventHandler Clicked;

        protected abstract IBrush Fill { get; set; }

        protected bool AllowRightClick = false;
        protected bool AllowRightClickEvenIfDisabled = false;

        bool _enabled = true;
        protected bool Enabled {
            get => _enabled;
            set {
                _enabled = value;

                mouseHeld = false;

                Fill = (IBrush)Application.Current.Styles.FindResource(Enabled
                    ? (mouseOver
                        ? "ThemeButtonOverBrush"
                        : "ThemeButtonEnabledBrush"
                    ) : "ThemeButtonDisabledBrush"
                );
            }
        }

        protected virtual void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => Clicked = null;

        bool mouseHeld = false;
        bool mouseOver = false;

        protected void MouseEnter(object sender, PointerEventArgs e) {
            if (Enabled) Fill = (IBrush)Application.Current.Styles.FindResource(mouseHeld? "ThemeButtonDownBrush" : "ThemeButtonOverBrush");
            mouseOver = true;
        }

        protected void MouseLeave(object sender, PointerEventArgs e) {
            if (Enabled) Fill = (IBrush)Application.Current.Styles.FindResource("ThemeButtonEnabledBrush");
            mouseHeld = mouseOver = false;
        }

        protected void MouseDown(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed || (AllowRightClick && MouseButton == PointerUpdateKind.RightButtonPressed)) {
                mouseHeld = true;

                if (Enabled) Fill = (IBrush)Application.Current.Styles.FindResource("ThemeButtonDownBrush");
            }
        }

        protected void MouseUp(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            bool isRightClick = AllowRightClick && MouseButton == PointerUpdateKind.RightButtonReleased;

            if (mouseHeld && (MouseButton == PointerUpdateKind.LeftButtonReleased || isRightClick)) {
                mouseHeld = false;

                MouseEnter(sender, null);

                if (Enabled || (isRightClick && AllowRightClickEvenIfDisabled)) Click(e);
            }
        }

        protected virtual void Click(PointerReleasedEventArgs e) => Clicked?.Invoke();
    }
}
