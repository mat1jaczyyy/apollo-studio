using System;
using System.Diagnostics;
using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Apollo.Viewers {
    public class RecentProjectInfo: UserControl {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Filename = this.Get<TextBlock>("Filename");
            Folder = this.Get<TextBlock>("Folder");
        }

        public delegate void ProjectInfoEventHandler(string path);
        public event ProjectInfoEventHandler Opened, Showed;

        public delegate void RemovedEventHandler(RecentProjectInfo sender, string path);
        public event RemovedEventHandler Removed;
        
        string _path;

        TextBlock Filename, Folder;
        ContextMenu InfoContextMenu;

        public RecentProjectInfo(string path) {
            InitializeComponent();
            
            _path = path;
            
            Filename.Text = Path.GetFileNameWithoutExtension(_path);
            Folder.Text = $"({Path.GetFileName(Path.GetDirectoryName(_path))})";

            InfoContextMenu = (ContextMenu)this.Resources["InfoContextMenu"];
            InfoContextMenu.AddHandler(MenuItem.ClickEvent, ContextMenu_Click);
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            Opened = null;
            
            InfoContextMenu.RemoveHandler(MenuItem.ClickEvent, ContextMenu_Click);
            InfoContextMenu = null;
        }

        protected void ContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item is MenuItem selected) {
                if ((string)selected.Header == "Remove") Removed?.Invoke(this, _path);
                else if ((string)selected.Header == "Open Containing Folder") Showed?.Invoke(Path.GetDirectoryName(_path));
            }
        }

        bool mouseHeld = false;

        void MouseLeave(object sender, PointerEventArgs e) => mouseHeld = false;

        void MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton == MouseButton.Left || e.MouseButton == MouseButton.Right)
                mouseHeld = true;
        }

        void MouseUp(object sender, PointerReleasedEventArgs e) {
            if (mouseHeld) {
                if (e.MouseButton == MouseButton.Left) Opened?.Invoke(_path);
                else if (e.MouseButton == MouseButton.Right) InfoContextMenu.Open((Control)sender);

                mouseHeld = false;
            }
        }
    }
}
