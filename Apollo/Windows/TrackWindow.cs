using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class TrackWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        Track _track;
        Grid Root;
        
        private void UpdateTitle(string path, int index) => this.Get<TextBlock>("Title").Text = (path == "")
            ? $"Track {index + 1}"
            : $"Track {index + 1} - {path}";

        private void UpdateTitle(string path) => UpdateTitle(path, _track.ParentIndex.Value);
        private void UpdateTitle(int index) => UpdateTitle(Program.Project.FilePath, index);
        private void UpdateTitle() => UpdateTitle(Program.Project.FilePath, _track.ParentIndex.Value);

        private void UpdateTopmost(bool value) => Topmost = value;

        private void UpdateContentAlignment(bool value) => Root.ColumnDefinitions[0] = new ColumnDefinition(1, value? GridUnitType.Star : GridUnitType.Auto);

        public ISelect SelectionStart { get; private set; } = null;
        public ISelect SelectionEnd { get; private set; } = null;

        public List<ISelect> Selection {
            get {
                if (SelectionStart != null) {
                    if (SelectionEnd != null) {
                        ISelect left = (SelectionStart.IParentIndex.Value < SelectionEnd.IParentIndex.Value)? SelectionStart : SelectionEnd;
                        ISelect right = (SelectionStart.IParentIndex.Value < SelectionEnd.IParentIndex.Value)? SelectionEnd : SelectionStart;

                        return left.IParent.IChildren.Skip(left.IParentIndex.Value).Take(right.IParentIndex.Value - left.IParentIndex.Value + 1).ToList();
                    }
                    
                    return new List<ISelect>() {SelectionStart};
                }

                return new List<ISelect>();
            }
        }

        public void Select(ISelect select, bool shift = false) {
            if (SelectionStart != null)
                if (SelectionEnd != null)
                    foreach (ISelect selected in Selection)
                        selected.IInfo?.Deselect();
                else SelectionStart.IInfo?.Deselect();

            if (shift && SelectionStart != null && SelectionStart.IParent == select.IParent && SelectionStart != select)
                SelectionEnd = select;

            else {
                SelectionStart = select;
                SelectionEnd = null;
            }

            if (SelectionStart != null)
                if (SelectionEnd != null)
                    foreach (ISelect selected in Selection)
                        selected.IInfo?.Select();
                else SelectionStart.IInfo?.Select();
        }

        public void SelectionAction(string action) {
            if (SelectionStart == null) return;

            ISelectParent parent = SelectionStart.IParent;
            
            int left = SelectionStart.IParentIndex.Value;
            int right = (SelectionEnd == null)? left: SelectionEnd.IParentIndex.Value;
            
            if (left > right) {
                int temp = left;
                left = right;
                right = temp;
            }

            SelectionAction(action, parent, left, right);
        }

        public void SelectionAction(string action, ISelectParent parent, int index) => SelectionAction(action, parent, index, index);

        public void SelectionAction(string action, ISelectParent parent, int left, int right) {
            if (action == "Cut") parent.IViewer?.Copy(left, right, true);
            else if (action == "Copy") parent.IViewer?.Copy(left, right);
            else if (action == "Duplicate") parent.IViewer?.Duplicate(left, right);
            else if (action == "Paste") parent.IViewer?.Paste(right);
            else if (action == "Delete") parent.IViewer?.Delete(left, right);
            else if (action == "Group") parent.IViewer?.Group(left, right);
            else if (action == "Ungroup") parent.IViewer?.Ungroup(left);
        }

        public TrackWindow(Track track) {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif

            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            _track = track;

            ChainViewer chainViewer = new ChainViewer(_track.Chain);

            Root = chainViewer.Get<Grid>("Layout");
            UpdateContentAlignment(Preferences.CenterTrackContents);
            Preferences.CenterTrackContentsChanged += UpdateContentAlignment;

            this.Get<ScrollViewer>("Contents").Content = chainViewer;
        }

        private void Loaded(object sender, EventArgs e) {
            Program.Project.PathChanged += UpdateTitle;
            _track.ParentIndexChanged += UpdateTitle;
            UpdateTitle();
        }

        private void Unloaded(object sender, EventArgs e) {
            _track.Window = null;
            
            Program.Project.PathChanged -= UpdateTitle;
            _track.ParentIndexChanged -= UpdateTitle;
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;
            Preferences.CenterTrackContentsChanged -= UpdateContentAlignment;

            Program.WindowClose(this);
        }

        private void Window_Focus(object sender, PointerPressedEventArgs e) => this.Focus();

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
        
        private void Minimize() => WindowState = WindowState.Minimized;

        private void ResizeWest(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.West);

        private void ResizeEast(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.East);

        public static void Create(Track track, Window owner) {
            if (track.Window == null) {
                track.Window = new TrackWindow(track) {Owner = owner};
                track.Window.Show();
                track.Window.Owner = null;
            } else {
                track.Window.WindowState = WindowState.Normal;
                track.Window.Activate();
            }
        }
    }
}