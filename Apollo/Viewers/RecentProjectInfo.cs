using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Apollo.Viewers {
    public class RecentProjectInfo: UserControl {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Path = this.Get<TextBlock>("Path");
        }

        public delegate void OpenedEventHandler(string path);
        public event OpenedEventHandler Opened;
        
        TextBlock Path;

        public RecentProjectInfo(string path) {
            InitializeComponent();
            
            Path.Text = path;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => Opened = null;

        void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) Opened?.Invoke(Path.Text);
        }
    }
}
