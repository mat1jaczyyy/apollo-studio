using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Apollo.Components {
    public class CollapseButton: IconButton {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Path = this.Get<Path>("Path");
            Rotation = this.Get<RotateTransform>("Rotation");
        }

        Path Path;
        RotateTransform Rotation;

        protected override IBrush Fill {
            get => Path.Stroke;
            set => Path.Stroke = value;
        }

        public bool Showing {
            get => Rotation.Angle == 180;
            set {
                Rotation.Angle = value? 180 : 0;
                base.MouseLeave(this, null);
            }
        }

        public CollapseButton() {
            InitializeComponent();

            base.MouseLeave(this, null);
        }
    }
}
