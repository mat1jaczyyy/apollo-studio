using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Apollo.Core;

namespace Apollo.Components {
    public class Close: IconButton {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Path = this.Get<Path>("Path");
        }

        public new delegate void ClickedEventHandler();
        public new event ClickedEventHandler Clicked;

        public delegate void ForceEventHandler(bool force);
        public event ForceEventHandler ClickedWithForce;

        Path Path;

        protected override IBrush Fill {
            get => Path.Stroke;
            set => Path.Stroke = value;
        }

        public Close() {
            InitializeComponent();

            base.MouseLeave(this, null);
        }

        protected override void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            base.Unloaded(sender, e);
            Clicked = null;
        }

        protected override void Click(PointerReleasedEventArgs e) {
            Clicked?.Invoke();
            ClickedWithForce?.Invoke(e.KeyModifiers == App.ControlKey);
        }
    }
}
