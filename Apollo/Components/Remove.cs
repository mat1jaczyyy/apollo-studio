using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Apollo.Components {
    public class Remove: IconButton {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        Path Path;

        protected override IBrush Fill {
            get => Path.Stroke;
            set => Path.Stroke = value;
        }
        
        public Remove() {
            InitializeComponent();

            Path = this.Get<Path>("Path");

            base.MouseLeave(this, null);
        }
    }
}
