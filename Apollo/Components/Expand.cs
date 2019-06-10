using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Apollo.Components {
    public class Expand: IconButton {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public new delegate void ClickedEventHandler(IPointerDevice e);
        public new event ClickedEventHandler Clicked;

        protected override IBrush Fill {
            get => (IBrush)this.Resources["Brush"];
            set => this.Resources["Brush"] = value;
        }

        public Expand() {
            InitializeComponent();

            base.MouseLeave(this, null);
        }

        protected override void Click(PointerReleasedEventArgs e) => Clicked?.Invoke(e.Device);
    }
}
