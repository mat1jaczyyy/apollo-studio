using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class HorizontalDial: Dial {
        protected override void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            ArcCanvas = this.Get<Canvas>("ArcCanvas");
            ArcBase = this.Get<Path>("ArcBase");
            Arc = this.Get<Path>("Arc");

            Display = this.Get<TextBlock>("Display");
            TitleText = this.Get<TextBlock>("Title");

            Input = this.Get<TextBox>("Input");
        }

        public HorizontalDial() {}
    }
}
