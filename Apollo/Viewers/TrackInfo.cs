using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Windows;

namespace Apollo.Viewers {
    public class TrackInfo: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void TrackAddedEventHandler(int index);
        public event TrackAddedEventHandler TrackAdded;
        
        Track _track;
        DropDown PortSelector;

        private void UpdateText(int index) => this.Get<TextBlock>("Name").Text = $"Track {index + 1}";

        private void UpdatePorts() {
            List<Launchpad> ports = (from i in MIDI.Devices where i.Available select i).ToList();
            if (_track.Launchpad != null && !_track.Launchpad.Available) ports.Add(_track.Launchpad);

            PortSelector.Items = ports;
            PortSelector.SelectedIndex = -1;
            PortSelector.SelectedItem = _track.Launchpad;
        }

        private void HandlePorts() => Dispatcher.UIThread.InvokeAsync((Action)UpdatePorts);
        
        public TrackInfo(Track track) {
            InitializeComponent();
            
            _track = track;
            
            UpdateText(_track.ParentIndex.Value);
            _track.ParentIndexChanged += UpdateText;

            PortSelector = this.Get<DropDown>("PortSelector");
            UpdatePorts();
            MIDI.DevicesUpdated += HandlePorts;
        }
        
        private void Clicked(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) TrackWindow.Create(_track, (Window)this.GetVisualRoot());
        }

        private void Track_Add() => TrackAdded?.Invoke(_track.ParentIndex.Value + 1);

        private void Track_Remove() {
            ((Panel)Parent).Children.RemoveAt(_track.ParentIndex.Value + 1);
            Program.Project.Remove(_track.ParentIndex.Value);
            _track.Window?.Close();
            _track.Dispose();
        }

        private void Port_Changed(object sender, SelectionChangedEventArgs e) {
            Launchpad selected = (Launchpad)PortSelector.SelectedItem;

            if (selected != null && _track.Launchpad != selected) {
                _track.Launchpad = selected;
                UpdatePorts();
            }
        }
    }
}
