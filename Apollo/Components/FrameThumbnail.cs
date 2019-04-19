using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Structures;

namespace Apollo.Components {
    public class FrameThumbnail: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void ClickedEventHandler();
        public event ClickedEventHandler Clicked;

        Frame _frame = new Frame();
        LaunchpadGrid Launchpad;
        public TextBlock Time;

        public Frame Frame {
            get => _frame;
            set {
                _frame = value;
                Launchpad.RenderFrame(_frame);
            }
        }

        public FrameThumbnail() {
            InitializeComponent();

            Launchpad = this.Get<LaunchpadGrid>("Launchpad");
            Time = this.Get<TextBlock>("Time");

            Launchpad.RenderFrame(_frame);
        }

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) Clicked?.Invoke();
        }
    }
}
