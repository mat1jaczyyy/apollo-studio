using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Structures;

namespace Apollo.Components {
    public class FrameThumbnail: UserControl {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Launchpad = this.Get<LaunchpadGrid>("Launchpad");
            Time = this.Get<TextBlock>("Time");
        }

        Frame _frame = new Frame();
        public LaunchpadGrid Launchpad;
        public TextBlock Time;

        public Frame Frame {
            get => _frame;
            set {
                _frame = value;
                Launchpad.RenderFrame(_frame);
                Time.Text = _frame.ToString();
            }
        }

        public FrameThumbnail() {
            InitializeComponent();

            Launchpad.RenderFrame(_frame);
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _frame = null;
    }
}
