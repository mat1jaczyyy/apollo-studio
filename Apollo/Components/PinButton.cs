using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

using Apollo.Core;

namespace Apollo.Components {
    public class PinButton: IconButton {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            PathUnpinned = this.Get<Path>("PathUnpinned");
            PathPinned = this.Get<Path>("PathPinned");
        }

        Path PathUnpinned, PathPinned;
        Path CurrentPath => Preferences.AlwaysOnTop? PathPinned : PathUnpinned;

        protected override IBrush Fill {
            get => CurrentPath.Stroke;
            set => PathUnpinned.Fill = PathUnpinned.Stroke = PathPinned.Fill = PathPinned.Stroke = value;
        }

        void UpdateTopmost(bool value) {
            (value? PathUnpinned : PathPinned).Opacity = 0;
            (value? PathPinned : PathUnpinned).Opacity = 1;
        }

        public PinButton() {
            InitializeComponent();

            base.MouseLeave(this, null);

            CurrentPath.Opacity = 1;

            Preferences.AlwaysOnTopChanged += UpdateTopmost;
        }

        protected override void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            base.Unloaded(sender, e);

            Preferences.AlwaysOnTopChanged -= UpdateTopmost;
        }
        
        protected override void Click(PointerReleasedEventArgs e) {
            Preferences.AlwaysOnTop = !Preferences.AlwaysOnTop;
            ((Window)this.GetVisualRoot()).Activate();
        }
    }
}
