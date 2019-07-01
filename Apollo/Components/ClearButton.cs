using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Apollo.Core;
using Apollo.Elements;

namespace Apollo.Components {
    public class ClearButton: IconButton {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Path = this.Get<Path>("Path");
        }

        Path Path;

        protected override IBrush Fill {
            get => Path.Fill;
            set => Path.Fill = value;
        }

        public ClearButton() {
            InitializeComponent();

            base.MouseLeave(this, null);
        }

        protected override void Click(PointerReleasedEventArgs e) {
            foreach (Launchpad lp in MIDI.Devices)
                lp.Clear(true);
        }
    }
}
