using System.Diagnostics;

using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Apollo.Components {
    public class Patreon: IconButton {
        public static readonly string URL = "https://patreon.com/mat1jaczyyy";

        void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        protected override IBrush Fill {
            get => (IBrush)this.Resources["Brush"];
            set => this.Resources["Brush"] = value;
        }

        public Patreon() {
            InitializeComponent();

            base.MouseLeave(this, null);
        }

        protected override void Click(PointerReleasedEventArgs e) => Process.Start(new ProcessStartInfo() {
            FileName = URL,
            UseShellExecute = true
        });
    }
}
