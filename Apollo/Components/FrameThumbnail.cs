using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Structures;

namespace Apollo.Components {
    public class FrameThumbnail: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void ClickedEventHandler();
        public event ClickedEventHandler Clicked;

        Frame _frame;
        LaunchpadGrid Launchpad;
        TextBlock Time;

        public FrameThumbnail(Frame frame) {
            InitializeComponent();

            _frame = frame;

            Launchpad = this.Get<LaunchpadGrid>("Launchpad");
            Launchpad.RenderFrame(_frame);

            Time = this.Get<TextBlock>("Time");
        }

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) Clicked?.Invoke();
        }
    }
}
