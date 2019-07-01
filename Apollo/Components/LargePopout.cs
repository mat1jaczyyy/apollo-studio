using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Apollo.Components {
    public class LargePopout: IconButton {
        void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        protected override IBrush Fill {
            get => (IBrush)this.Resources["Brush"];
            set => this.Resources["Brush"] = value;
        }

        public LargePopout() {
            InitializeComponent();

            base.MouseLeave(this, null);
        }
    }
}
