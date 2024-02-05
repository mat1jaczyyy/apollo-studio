using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Apollo.Core;

namespace Apollo.Components {
    public class mat1jaczyyy: IconButton {
        public static readonly string URL = "https://mat1jaczyyy.com";

        void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        protected override IBrush Fill {
            get => (IBrush)this.Resources["Brush"];
            set => this.Resources["Brush"] = value;
        }

        public mat1jaczyyy() {
            InitializeComponent();

            base.MouseLeave(this, null);
        }

        protected override void Click(PointerReleasedEventArgs e) => App.URL(URL);
    }
}
