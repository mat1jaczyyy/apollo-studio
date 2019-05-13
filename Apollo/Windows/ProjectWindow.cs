using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

using Apollo.Binary;
using Apollo.Components;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class ProjectWindow: Window, ISelectParentViewer {
        public int? IExpanded {
            get => null;
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void UpdateTitle() => TitleText.Text = (Program.Project.FilePath == "")? "New Project" : Program.Project.FilePath;

        private void UpdatePage() => Page.RawValue = Program.Project.Page;
        private void HandlePage() => Dispatcher.UIThread.InvokeAsync((Action)UpdatePage);

        private void UpdateTopmost(bool value) => Topmost = value;

        TextBlock TitleText;

        ContextMenu TrackContextMenu;
        Controls Contents;
        TrackAdd TrackAdd;

        TextBox BPM;
        HorizontalDial Page;
        
        private void SetAlwaysShowing() {
            TrackAdd.AlwaysShowing = (Contents.Count == 1);

            for (int i = 1; i < Contents.Count; i++)
                ((TrackInfo)Contents[i]).TrackAdd.AlwaysShowing = false;

            if (Contents.Count > 1) ((TrackInfo)Contents.Last()).TrackAdd.AlwaysShowing = true;
        }

        public void Contents_Insert(int index, Track track) {
            TrackInfo viewer = new TrackInfo(track);
            viewer.TrackAdded += Track_Insert;
            viewer.TrackRemoved += Track_Remove;
            track.Info = viewer;

            Contents.Insert(index + 1, viewer);
            SetAlwaysShowing();
        }

        public void Contents_Remove(int index) {
            Contents.RemoveAt(index + 1);
            SetAlwaysShowing();
        }

        public SelectionManager Selection = new SelectionManager();
        
        public ProjectWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            TitleText = this.Get<TextBlock>("Title");

            TrackContextMenu = (ContextMenu)this.Resources["TrackContextMenu"];
            TrackContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(TrackContextMenu_Click));
            
            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);

            Contents = this.Get<StackPanel>("Contents").Children;
            TrackAdd = this.Get<TrackAdd>("TrackAdd");

            for (int i = 0; i < Program.Project.Count; i++)
                Contents_Insert(i, Program.Project[i]);
            
            BPM = this.Get<TextBox>("BPM");
            BPM.Text = Program.Project.BPM.ToString(CultureInfo.InvariantCulture);

            BPM.GetObservable(TextBox.TextProperty).Subscribe(BPM_Changed);

            Page = this.Get<HorizontalDial>("Page");
            Page.RawValue = Program.Project.Page;
        }
        
        public void Expand(int? index) {}
        
        private void Loaded(object sender, EventArgs e) {
            Program.Project.PathChanged += UpdateTitle;
            UpdateTitle();

            Program.Project.PageChanged += HandlePage;
            UpdatePage();
        }

        private void Unloaded(object sender, EventArgs e) {
            Program.Project.Window = null;

            Program.Project.PathChanged -= UpdateTitle;
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;

            Program.WindowClose(this);
        }

        private void Track_Insert(int index) => Track_Insert(index, new Track());

        private void Track_Insert(int index, Track track) {
            Program.Project.Insert(index, track);
            Contents_Insert(index, Program.Project[index]);
            
            Selection.Select(Program.Project[index]);
        }

        private void Track_InsertStart() => Track_Insert(0);

        private void Track_Remove(int index) {
            Contents_Remove(index);
            Program.Project[index].Window?.Close();
            Program.Project.Remove(index);
            
            if (index < Program.Project.Count)
                Selection.Select(Program.Project[index]);
            else if (Program.Project.Count > 0)
                Selection.Select(Program.Project.Tracks.Last());
            else
                Selection.Select(null);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (Selection.Start == null) return;

            if (Selection.ActionKey(e)) return;

            if (e.Key == Key.Up) Selection.Move(false, e.Modifiers == InputModifiers.Shift);
            else if (e.Key == Key.Down) Selection.Move(true, e.Modifiers == InputModifiers.Shift);
            else if (e.Key == Key.Enter)
                foreach (ISelect i in Selection.Selection)
                    TrackWindow.Create((Track)i, this);
        }

        private void Page_Changed(double value) => Program.Project.Page = (int)value;

        private Action BPM_Update;

        private void BPM_Changed(string text) {
            if (text == null) return;
            if (text == "") text = "0";

            BPM_Update = () => { BPM.Text = Program.Project.BPM.ToString(CultureInfo.InvariantCulture); };

            if (int.TryParse(text, out int value)) {
                if (20 <= value && value <= 999) {
                    Program.Project.BPM = value;
                    BPM_Update = () => { BPM.Foreground = (IBrush)Application.Current.Styles.FindResource("ThemeForegroundBrush"); };
                } else {
                    BPM_Update = () => { BPM.Foreground = (IBrush)Application.Current.Styles.FindResource("ErrorBrush"); };
                }

                BPM_Update += () => { 
                    if (value <= 0) text = "0";
                    else text = text.TrimStart('0');

                    if (value > 999) text = "999";
                    
                    BPM.Text = text;
                };
            }

            Dispatcher.UIThread.InvokeAsync(() => {
                BPM_Update?.Invoke();
                BPM_Update = null;
            });
        }
        
        private void BPM_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Return) this.Focus();
        }

        private void Window_Focus(object sender, PointerPressedEventArgs e) => this.Focus();

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
        
        private void Minimize() => WindowState = WindowState.Minimized;

        private void CheckClose(bool force) {
            if (force) foreach (Track track in Program.Project.Tracks) track.Window?.Close();
            Close();
        }

        private void ResizeNorth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.North);

        private void ResizeSouth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.South);

        public static void Create(Window owner) {
            if (Program.Project.Window == null) {
                Program.Project.Window = new ProjectWindow() {Owner = owner};
                Program.Project.Window.Show();
                Program.Project.Window.Owner = null;
            } else {
                Program.Project.Window.WindowState = WindowState.Normal;
                Program.Project.Window.Activate();
            }
        }

        private void Track_Action(string action) => Track_Action(action, false);
        private void Track_Action(string action, bool right) => Program.Project.Window?.Selection.Action(action, Program.Project, (right? Program.Project.Count : 0) - 1);

        private void TrackContextMenu_Click(object sender, EventArgs e) {
            this.Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Track_Action((string)((MenuItem)item).Header, true);
        }

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Right)
                TrackContextMenu.Open((Control)sender);

            e.Handled = true;
        }

        private void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.Contains("track")) e.DragEffects = DragDropEffects.None; 
        }

        private void Drop(object sender, DragEventArgs e) {
            e.Handled = true;

            if (!e.Data.Contains("track")) return;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZoneAfter" && source.Name != "TrackAdd")
                source = source.Parent;

            List<Track> moving = ((List<ISelect>)e.Data.Get("track")).Select(i => (Track)i).ToList();
            bool copy = e.Modifiers.HasFlag(InputModifiers.Control);

            bool result;

            if (source.Name != "DropZoneAfter" || Program.Project.Count == 0) result = Track.Move(moving, Program.Project, copy);
            else result = Track.Move(moving, Program.Project.Tracks.Last(), copy);

            if (!result) e.DragEffects = DragDropEffects.None;
        }

        public async void Copy(int left, int right, bool cut = false) {
            Copyable copy = new Copyable();
            
            for (int i = left; i <= right; i++)
                copy.Contents.Add(Program.Project[i]);

            string b64 = Convert.ToBase64String(Encoder.Encode(copy).ToArray());

            if (cut) Delete(left, right);
            
            await Application.Current.Clipboard.SetTextAsync(b64);
        }

        public async void Paste(int right) {
            string b64 = await Application.Current.Clipboard.GetTextAsync();
            
            Copyable paste = Decoder.Decode(new MemoryStream(Convert.FromBase64String(b64)), typeof(Copyable));
            
            for (int i = 0; i < paste.Contents.Count; i++)
                Track_Insert(right + i + 1, (Track)paste.Contents[i]);
        }

        public void Duplicate(int left, int right) {
            for (int i = 0; i <= right - left; i++)
                Track_Insert(right + i + 1, Program.Project[left + i].Clone());
        }

        public void Delete(int left, int right) {
            for (int i = right; i >= left; i--)
                Track_Remove(i);
        }

        public void Group(int left, int right) => throw new InvalidOperationException("A Track cannot be grouped.");

        public void Ungroup(int index) => throw new InvalidOperationException("A Track cannot be ungrouped.");

        public void Rename(int left, int right) => ((TrackInfo)Contents[left + 1]).StartInput(left, right);
    }
}