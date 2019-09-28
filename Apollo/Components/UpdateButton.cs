using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Apollo.Components {
    public class UpdateButton: IconButton {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Path = this.Get<Path>("Path");
        }

        Path Path;

        protected override IBrush Fill {
            get => Path.Stroke;
            set => Path.Stroke = value;
        }

        public UpdateButton() {
            InitializeComponent();

            base.MouseLeave(this, null);

            Opacity = 0;
            IsHitTestVisible = false;
        }

        public void Enable() {
            Opacity = 1;
            IsHitTestVisible = true;
        }
    }
}
