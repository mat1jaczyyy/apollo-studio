using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Apollo.Components {
    public class Close: IconButton {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public new delegate void ClickedEventHandler(bool force);
        public new event ClickedEventHandler Clicked;

        Path Path;

        protected override IBrush Fill {
            get => Path.Stroke;
            set => Path.Stroke = value;
        }

        public Close() {
            InitializeComponent();

            Path = this.Get<Path>("Path");

            base.MouseLeave(this, null);
        }

        protected override void Click(InputModifiers e) => Clicked?.Invoke(e == InputModifiers.Control);
    }
}
