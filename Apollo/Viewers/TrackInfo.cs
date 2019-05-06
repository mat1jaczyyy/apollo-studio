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
using Apollo.Windows;

namespace Apollo.Viewers {
    public class TrackInfo: UserControl, ISelectViewer {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void TrackInfoEventHandler(int index);
        public event TrackInfoEventHandler TrackAdded;
        public event TrackInfoEventHandler TrackRemoved;
        
        Track _track;
        bool selected = false;

        TextBlock NameText;
        ComboBox PortSelector;
        public TrackAdd TrackAdd;

        Border DropZone;
        ContextMenu TrackContextMenu;
        TextBox Input;

        private void UpdateText(int index) => NameText.Text = $"Track {index + 1}";

        private void UpdatePorts() {
            List<Launchpad> ports = (from i in MIDI.Devices where i.Available && i.Type != Launchpad.LaunchpadType.Unknown select i).ToList();
            if (_track.Launchpad != null && (!_track.Launchpad.Available || _track.Launchpad.Type == Launchpad.LaunchpadType.Unknown)) ports.Add(_track.Launchpad);

            PortSelector.Items = ports;
            PortSelector.SelectedIndex = -1;
            PortSelector.SelectedItem = _track.Launchpad;
        }

        private void HandlePorts() => Dispatcher.UIThread.InvokeAsync((Action)UpdatePorts);
        
        private void ApplyHeaderBrush(string resource) {
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

        public TrackInfo(Track track) {
            InitializeComponent();
            
            _track = track;

            NameText = this.Get<TextBlock>("Draggable");
            UpdateText(_track.ParentIndex.Value);
            _track.ParentIndexChanged += UpdateText;

            PortSelector = this.Get<ComboBox>("PortSelector");
            UpdatePorts();
            MIDI.DevicesUpdated += HandlePorts;

            TrackAdd = this.Get<TrackAdd>("DropZoneAfter");

            TrackContextMenu = (ContextMenu)this.Resources["TrackContextMenu"];
            TrackContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(ContextMenu_Click));

            DropZone = this.Get<Border>("DropZone");
            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);
            
            Deselect();

            //Input = this.Get<TextBox>("Input");
            //Input.GetObservable(TextBox.TextProperty).Subscribe(Input_Changed);
        }
        
        private void Track_Action(string action) => Program.Project.Window?.Selection.Action(action, Program.Project, _track.ParentIndex.Value);

        private void ContextMenu_Click(object sender, EventArgs e) {
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Program.Project.Window?.Selection.Action((string)((MenuItem)item).Header);
        }

        private void Select(PointerPressedEventArgs e) {
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
                
                if (e.MouseButton == MouseButton.Right)
                    TrackContextMenu.Open(NameText);
            }
        }

        public void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.Contains("track")) e.DragEffects = DragDropEffects.None; 
        }

        public void Drop(object sender, DragEventArgs e) {
            e.Handled = true;

            if (!e.Data.Contains("track")) return;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZone" && source.Name != "DropZoneAfter")
                source = source.Parent;

            List<Track> moving = ((List<ISelect>)e.Data.Get("track")).Select(i => (Track)i).ToList();
            bool copy = e.Modifiers.HasFlag(InputModifiers.Control);

            bool result;
            
            if (source.Name == "DropZone" && e.GetPosition(source).Y < source.Bounds.Height / 2) {
                if (_track.ParentIndex == 0) result = Track.Move(moving, Program.Project, copy);
                else result = Track.Move(moving, Program.Project[_track.ParentIndex.Value - 1], copy);
            } else result = Track.Move(moving, _track, copy);

            if (!result) e.DragEffects = DragDropEffects.None;
        }

        private void Track_Add() => TrackAdded?.Invoke(_track.ParentIndex.Value + 1);

        private void Track_Remove() => TrackRemoved?.Invoke(_track.ParentIndex.Value);

        private void Port_Changed(object sender, SelectionChangedEventArgs e) {
            Launchpad selected = (Launchpad)PortSelector.SelectedItem;

            if (selected != null && _track.Launchpad != selected) {
                _track.Launchpad = selected;
                UpdatePorts();
            }
        }
    }
}
