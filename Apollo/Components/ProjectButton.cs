using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

using Apollo.Windows;

namespace Apollo.Components {
    public class ProjectButton: IconButton {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        Path Path;

        protected override IBrush Fill {
            get => Path.Fill;
            set => Path.Fill = value;
        }

        public ProjectButton() {
            InitializeComponent();

            Path = this.Get<Path>("Path");

            base.MouseLeave(this, null);
        }

        protected override void Click(PointerReleasedEventArgs e) => ProjectWindow.Create((Window)this.GetVisualRoot());
    }
}
