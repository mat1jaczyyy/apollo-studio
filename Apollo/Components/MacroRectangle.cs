using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Apollo.Components {
    public class MacroRectangle: UserControl {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Rect = this.Get<Grid>("Rect");
            Text = this.Get<TextBlock>("Text");
        }

        Grid Rect;
        TextBlock Text;

        public IBrush Fill {
            get => Rect.Background;
            set => Rect.Background = value;
        }

        public int Index {
            set => Text.Text = value.ToString();
        }

        public MacroRectangle() => InitializeComponent();
    }
}
