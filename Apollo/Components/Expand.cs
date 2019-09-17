using Avalonia;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Apollo.Components {
    public class Expand: IconButton {
        void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public new delegate void ClickedEventHandler(PointerEventArgs e);
        public new event ClickedEventHandler Clicked;

        protected override IBrush Fill {
            get => (IBrush)this.Resources["Brush"];
            set => this.Resources["Brush"] = value;
        }

        public Expand() {
            InitializeComponent();

            base.MouseLeave(this, null);
        }

        protected override void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            base.Unloaded(sender, e);
            Clicked = null;
        }

        protected override void Click(PointerReleasedEventArgs e) => Clicked?.Invoke(e);
    }
}
