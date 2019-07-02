using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Apollo.Viewers {
    public class RecentProjectInfo: UserControl {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Filename = this.Get<TextBlock>("Filename");
            Folder = this.Get<TextBlock>("Folder");
        }

        public delegate void OpenedEventHandler(string path);
        public event OpenedEventHandler Opened;
        
        string _path;

        TextBlock Filename, Folder;

        public RecentProjectInfo(string path) {
            InitializeComponent();
            
            _path = path;
            
            Filename.Text = Path.GetFileNameWithoutExtension(_path);
            Folder.Text = $"({Path.GetFileName(Path.GetDirectoryName(_path))})";
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => Opened = null;

        bool mouseHeld = false;

        void MouseLeave(object sender, PointerEventArgs e) => mouseHeld = false;

        void MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton == MouseButton.Left || e.MouseButton == MouseButton.Right)
                mouseHeld = true;
        }

        void MouseUp(object sender, PointerReleasedEventArgs e) {
            if (mouseHeld) {
                if (e.MouseButton == MouseButton.Left) Opened?.Invoke(_path);
                else if (e.MouseButton == MouseButton.Right) {}

                mouseHeld = false;
            }
        }
    }
}
