using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

using Apollo.Windows;

namespace Apollo.Components {
    public class PreferencesButton: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        Ellipse Hole;

        public IBrush Fill {
            get => Hole.Fill;
            set => Hole.Fill = value;
        }

        public PreferencesButton() {
            InitializeComponent();

            Hole = this.Get<Ellipse>("Hole");
            Hole.Fill = (SolidColorBrush)Application.Current.Styles.FindResource("ThemeBorderMidBrush");
        }

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) PreferencesWindow.Create((Window)this.GetVisualRoot());
        }
    }
}
