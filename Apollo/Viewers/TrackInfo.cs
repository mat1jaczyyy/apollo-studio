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
using Apollo.DragDrop;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Selection;
using Apollo.Windows;

namespace Apollo.Viewers {
    public class TrackInfo: UserControl, ISelectViewer, IDraggable {
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
        
        IDisposable observable;

        public delegate void AddedEventHandler(int index);
        public event AddedEventHandler Added;
        
        Track _track;
        public bool Selected { get; private set; } = false;

        TextBlock NameText;
        ComboBox PortSelector;
        public TrackAdd TrackAdd;

        Grid Draggable;
        Border DropZone;
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
            Selected = true;
        }

        public void Deselect() {
            ApplyHeaderBrush("ThemeControlHighBrush");
            Selected = false;
        }

        public TrackInfo() => new InvalidOperationException();

        public TrackInfo(Track track) {
            InitializeComponent();
            
            _track = track;

            UpdateText(0);
            _track.ParentIndexChanged += UpdateText;

            UpdatePorts();
            MIDI.DevicesUpdated += HandlePorts;

            DragDrop = new DragDropManager(this);
            
            Deselect();

            observable = Input.GetObservable(TextBox.TextProperty).Subscribe(Input_Changed);

            SetEnabled();
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            Added = null;
            
            MIDI.DevicesUpdated -= HandlePorts;

            _track.ParentIndexChanged -= UpdateText;
            _track.Info = null;
            _track = null;

            observable.Dispose();

            DragDrop.Dispose();
            DragDrop = null;
        }

        public virtual void SetEnabled() => NameText.Foreground = PortSelector.Foreground = (IBrush)Application.Current.Styles.FindResource(_track.Enabled? "ThemeForegroundBrush" : "ThemeForegroundLowBrush");
        
        void Track_Action(string action) => Program.Project.Window?.Selection.Action(action, Program.Project, _track.ParentIndex.Value);

        void ContextMenu_Action(string action) => Program.Project.Window?.Selection.Action(action);

        public void Select(PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed || (MouseButton == PointerUpdateKind.RightButtonPressed && !Selected))
                Program.Project.Window?.Selection.Select(_track, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
        }

        DragDropManager DragDrop;

        public string DragFormat => "Track";
        public List<string> DropAreas => new List<string>() {"DropZone", "DropZoneAfter"};

        public Dictionary<string, DragDropManager.DropHandler> DropHandlers => new Dictionary<string, DragDropManager.DropHandler>() {
            {DataFormats.FileNames, null},
            {DragFormat, null},
        };

        public ISelect Item => _track;
        public ISelectParent ItemParent => Program.Project;

        public void DragFailed(PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;
                
            if (MouseButton == PointerUpdateKind.LeftButtonPressed && e.ClickCount == 2) 
                TrackWindow.Create(_track, (Window)this.GetVisualRoot());
            
            if (MouseButton == PointerUpdateKind.RightButtonPressed) {
                MuteItem.Header = ((Track)Program.Project.Window?.Selection.Selection.First()).Enabled? "Mute" : "Unmute";
                ((ApolloContextMenu)this.Resources["TrackContextMenu"]).Open(Draggable);
            }
        }

        public void Drag(object sender, PointerPressedEventArgs e) => DragDrop.Drag(Program.Project.Window?.Selection, e);

        void Track_Add() => Added?.Invoke(_track.ParentIndex.Value + 1);

        void Port_Changed(object sender, SelectionChangedEventArgs e) {
            Launchpad selected = (Launchpad)PortSelector.SelectedItem;

            if (selected != null && _track.Launchpad != selected)
                Program.Project.Undo.AddAndExecute(new Track.LaunchpadChangedUndoEntry(
                    _track,
                    _track.Launchpad,
                    selected
                ));
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
            Input.CaretIndex = Input.Text.Length;

            Input.Opacity = 1;
            Input.IsHitTestVisible = true;
            Input.Focus();
        }
        
        void Input_LostFocus(object sender, RoutedEventArgs e) {
            Input.Text = _track.Name;

            Input.Opacity = 0;
            Input.IsHitTestVisible = false;
            
            List<string> newName = (from i in Enumerable.Range(0, Input_Clean.Count) select Input.Text).ToList();

            if (!newName.SequenceEqual(Input_Clean))
                Program.Project.Undo.Add(new Track.RenamedUndoEntry(
                    Input_Left,
                    Input_Right,
                    Input_Clean,
                    newName
                ));
        }

        public void SetName(string name) {
            UpdateText(0);

            if (Input_Ignore) return;

            Input_Ignore = true;
            Input.Text = name;
            Input_Ignore = false;
        }

        void Input_KeyDown(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            if (e.Key == Key.Return)
                this.Focus();

            e.Key = Key.None;
        }

        void Input_KeyUp(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            e.Key = Key.None;
        }

        void Input_MouseUp(object sender, PointerReleasedEventArgs e) => e.Handled = true;
    }
}
