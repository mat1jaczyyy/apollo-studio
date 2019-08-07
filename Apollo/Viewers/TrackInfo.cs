using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

using Apollo.Components;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Interfaces;
using Apollo.Windows;

namespace Apollo.Viewers {
    public class TrackInfo: UserControl, ISelectViewer {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            NameText = this.Get<TextBlock>("Name");
            Draggable = this.Get<Grid>("Draggable");

            PortSelector = this.Get<ComboBox>("PortSelector");
            DropZone = this.Get<Border>("DropZone");
            TrackAdd = this.Get<TrackAdd>("DropZoneAfter");
            MuteItem = this.Get<MenuItem>("MuteItem");
            Input = this.Get<TextBox>("Input");
        }

        public delegate void AddedEventHandler(int index);
        public event AddedEventHandler Added;
        
        Track _track;
        bool selected = false;

        TextBlock NameText;
        ComboBox PortSelector;
        public TrackAdd TrackAdd;

        Grid Draggable;
        Border DropZone;
        ContextMenu TrackContextMenu;
        MenuItem MuteItem;
        TextBox Input;
        
        void UpdateText(int index) => NameText.Text = _track.ProcessedName;

        public void UpdatePorts() {
            List<Launchpad> ports = (from i in MIDI.Devices where i.Available && i.Type != LaunchpadType.Unknown select i).ToList();
            if (_track.Launchpad != null && (!_track.Launchpad.Available || _track.Launchpad.Type == LaunchpadType.Unknown)) ports.Add(_track.Launchpad);
            ports.Add(MIDI.NoOutput);

            PortSelector.Items = ports;
            PortSelector.SelectedIndex = -1;
            PortSelector.SelectedItem = _track.Launchpad;
        }

        void HandlePorts() => Dispatcher.UIThread.InvokeAsync((Action)UpdatePorts);
        
        void ApplyHeaderBrush(string resource) {
            IBrush brush = (IBrush)Application.Current.Styles.FindResource(resource);

            if (IsArrangeValid) DropZone.Background = brush;
            else this.Resources["BackgroundBrush"] = brush;
        }

        public void Select() {
            ApplyHeaderBrush("ThemeAccentBrush2");
            selected = true;
        }

        public void Deselect() {
            ApplyHeaderBrush("ThemeControlHighBrush");
            selected = false;
        }

        public TrackInfo() => new InvalidOperationException();

        public TrackInfo(Track track) {
            InitializeComponent();
            
            _track = track;

            UpdateText(0);
            _track.ParentIndexChanged += UpdateText;

            UpdatePorts();
            MIDI.DevicesUpdated += HandlePorts;

            TrackContextMenu = (ContextMenu)this.Resources["TrackContextMenu"];
            TrackContextMenu.AddHandler(MenuItem.ClickEvent, ContextMenu_Click);

            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);
            
            Deselect();

            Input.GetObservable(TextBox.TextProperty).Subscribe(Input_Changed);

            SetEnabled();
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            Added = null;
            
            MIDI.DevicesUpdated -= HandlePorts;

            _track.ParentIndexChanged -= UpdateText;
            _track.Info = null;
            _track = null;

            TrackContextMenu.RemoveHandler(MenuItem.ClickEvent, ContextMenu_Click);
            TrackContextMenu = null;

            this.RemoveHandler(DragDrop.DropEvent, Drop);
            this.RemoveHandler(DragDrop.DragOverEvent, DragOver);
        }

        public virtual void SetEnabled() => NameText.Foreground = PortSelector.Foreground = (IBrush)Application.Current.Styles.FindResource(_track.Enabled? "ThemeForegroundBrush" : "ThemeForegroundLowBrush");
        
        void Track_Action(string action) => Program.Project.Window?.Selection.Action(action, Program.Project, _track.ParentIndex.Value);

        void ContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Program.Project.Window?.Selection.Action((string)((MenuItem)item).Header);
        }

        void Select(PointerPressedEventArgs e) {
            if (e.MouseButton == MouseButton.Left || (e.MouseButton == MouseButton.Right && !selected))
                Program.Project.Window?.Selection.Select(_track, e.InputModifiers.HasFlag(InputModifiers.Shift));
        }

        public async void Drag(object sender, PointerPressedEventArgs e) {
            if (!selected) Select(e);

            DataObject dragData = new DataObject();
            dragData.Set("track", Program.Project.Window?.Selection.Selection);

            DragDropEffects result = await DragDrop.DoDragDrop(dragData, DragDropEffects.Move);

            if (result == DragDropEffects.None) {
                if (selected) Select(e);
                
                if (e.MouseButton == MouseButton.Left && e.ClickCount == 2) 
                    TrackWindow.Create(_track, (Window)this.GetVisualRoot());
                
                if (e.MouseButton == MouseButton.Right) {
                    MuteItem.Header = ((Track)Program.Project.Window?.Selection.Selection.First()).Enabled? "Mute" : "Unmute";
                    TrackContextMenu.Open(Draggable);
                }
            }
        }

        public void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.Contains("track") && !e.Data.Contains(DataFormats.FileNames)) e.DragEffects = DragDropEffects.None; 
        }

        public void Drop(object sender, DragEventArgs e) {
            e.Handled = true;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZone" && source.Name != "DropZoneAfter") {
                source = source.Parent;
                
                if (source == this) {
                    e.Handled = false;
                    return;
                }
            }

            int after = _track.ParentIndex.Value;
            if (source.Name == "DropZone" && e.GetPosition(source).Y < source.Bounds.Height / 2) after--;

            if (e.Data.Contains(DataFormats.FileNames)) {
                string path = e.Data.GetFileNames().FirstOrDefault();

                if (path != null) Program.Project.Window?.Import(after, path);

                return;
            }

            if (!e.Data.Contains("track")) return;

            List<Track> moving = ((List<ISelect>)e.Data.Get("track")).Select(i => (Track)i).ToList();
            int before = moving[0].IParentIndex.Value - 1;
            bool copy = e.Modifiers.HasFlag(App.ControlKey);

            bool result = Track.Move(moving, Program.Project, after, copy);

            if (result) {
                int before_pos = before;
                int after_pos = moving[0].IParentIndex.Value - 1;
                int count = moving.Count;

                if (after < before)
                    before_pos += count;
                
                Program.Project.Undo.Add($"Track {(copy? "Copied" : "Moved")}", copy
                    ? new Action(() => {
                        for (int i = after + count; i > after; i--)
                            Program.Project.Remove(i);

                    }) : new Action(() => {
                        List<Track> umoving = (from i in Enumerable.Range(after_pos + 1, count) select Program.Project[i]).ToList();

                        Track.Move(umoving, Program.Project, before_pos);

                }), () => {
                    List<Track> rmoving = (from i in Enumerable.Range(before + 1, count) select Program.Project[i]).ToList();

                    Track.Move(rmoving, Program.Project, after, copy);
                });
            
            } else e.DragEffects = DragDropEffects.None;
        }

        void Track_Add() => Added?.Invoke(_track.ParentIndex.Value + 1);

        void Port_Changed(object sender, SelectionChangedEventArgs e) {
            Launchpad selected = (Launchpad)PortSelector.SelectedItem;

            if (selected != null && _track.Launchpad != selected) {
                Launchpad u = _track.Launchpad;
                Launchpad r = selected;
                int path = _track.ParentIndex.Value;

                Program.Project.Undo.Add($"{_track.ProcessedName} Launchpad Changed to {selected.Name}", () => {
                    Program.Project[path].Launchpad = u;
                }, () => {
                    Program.Project[path].Launchpad = r;
                });

                _track.Launchpad = selected;
            }
        }

        int Input_Left, Input_Right;
        List<string> Input_Clean;
        bool Input_Ignore = false;

        void Input_Changed(string text) {
            if (text == null) return;
            if (text == "") return;

            if (Input_Ignore) return;

            Input_Ignore = true;
            for (int i = Input_Left; i <= Input_Right; i++)
                Program.Project[i].Name = text;
            Input_Ignore = false;
        }

        public void StartInput(int left, int right) {
            Input_Left = left;
            Input_Right = right;

            Input_Clean = new List<string>();
            for (int i = left; i <= right; i++)
                Input_Clean.Add(Program.Project[i].Name);

            Input.Text = _track.Name;
            Input.SelectionStart = 0;
            Input.SelectionEnd = Input.Text.Length;

            Input.Opacity = 1;
            Input.IsHitTestVisible = true;
            Input.Focus();
        }
        
        void Input_LostFocus(object sender, RoutedEventArgs e) {
            Input.Text = _track.Name;

            Input.Opacity = 0;
            Input.IsHitTestVisible = false;

            List<string> r = (from i in Enumerable.Range(0, Input_Clean.Count) select Input.Text).ToList();

            if (!r.SequenceEqual(Input_Clean)) {
                int left = Input_Left;
                int right = Input_Right;
                List<string> u = (from i in Input_Clean select i).ToList();

                Program.Project.Undo.Add($"Track Renamed to {Input.Text}", () => {
                    for (int i = left; i <= right; i++)
                        Program.Project[i].Name = u[i - left];

                    Program.Project.Window?.Selection.Select(Program.Project[left]);
                    Program.Project.Window?.Selection.Select(Program.Project[right], true);
                    
                }, () => {
                    for (int i = left; i <= right; i++)
                        Program.Project[i].Name = r[i - left];
                    
                    Program.Project.Window?.Selection.Select(Program.Project[left]);
                    Program.Project.Window?.Selection.Select(Program.Project[right], true);
                });
            }
        }

        public void SetName(string name) {
            UpdateText(0);

            if (Input_Ignore) return;

            Input_Ignore = true;
            Input.Text = name;
            Input_Ignore = false;
        }

        void Input_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Return)
                this.Focus();

            e.Key = Key.None;
        }

        void Input_KeyUp(object sender, KeyEventArgs e) => e.Key = Key.None;

        void Input_MouseUp(object sender, PointerReleasedEventArgs e) => e.Handled = true;
    }
}
