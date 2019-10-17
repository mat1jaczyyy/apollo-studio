using System;
using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using Apollo.Windows;

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

        public RecentProjectInfo() => new InvalidOperationException();

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
            Showed = null;
            Removed = null;
            
            InfoContextMenu.RemoveHandler(MenuItem.ClickEvent, ContextMenu_Click);
            InfoContextMenu = null;
        }

        async void ContextMenu_Click(object sender, EventArgs e) {
            Window root = (Window)this.GetVisualRoot();
            root.Focus();

            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item is MenuItem selected) {
                bool remove = (string)selected.Header == "Remove";

                if ((string)selected.Header == "Open Containing Folder") {
                    if (File.Exists(_path)) Showed?.Invoke(Path.GetDirectoryName(_path));
                    else remove |= await MessageWindow.Create(
                        $"An error occurred while locating the file.\n\n" +
                        "You may not have sufficient privileges to read from the destination folder, or\n" +
                        "the file you're attempting to locate has been moved.\n\n" +
                        "Would you like to remove it from the Recent Projects list?",
                        new string[] {"Yes", "No"}, root
                    ) == "Yes";
                }
                
                if (remove) Removed?.Invoke(this, _path);
            }
        }

        bool mouseHeld = false;

        void MouseLeave(object sender, PointerEventArgs e) => mouseHeld = false;

        void MouseDown(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed || MouseButton == PointerUpdateKind.RightButtonPressed)
                mouseHeld = true;
        }

        async void MouseUp(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (mouseHeld) {
                if (MouseButton == PointerUpdateKind.LeftButtonReleased) {
                    bool remove = false;

                    if (File.Exists(_path)) Opened?.Invoke(_path);
                    else remove = await MessageWindow.Create(
                        $"An error occurred while locating the file.\n\n" +
                        "You may not have sufficient privileges to read from the destination folder, or\n" +
                        "the file you're attempting to locate has been moved.\n\n" +
                        "Would you like to remove it from the Recent Projects list?",
                        new string[] {"Yes", "No"}, (Window)this.GetVisualRoot()
                    ) == "Yes";
                
                    if (remove) Removed?.Invoke(this, _path);

                } else if (MouseButton == PointerUpdateKind.RightButtonReleased) InfoContextMenu.Open((Control)sender);

                mouseHeld = false;
            }
        }
    }
}
