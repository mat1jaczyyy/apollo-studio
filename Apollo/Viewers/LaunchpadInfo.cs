using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;

using Apollo.Components;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Windows;

namespace Apollo.Viewers {
    public class LaunchpadInfo: UserControl {
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Popout = this.Get<Popout>("Popout");
            Rotation = this.Get<ComboBox>("Rotation");
            InputFormatSelector = this.Get<ComboBox>("InputFormatSelector");
            TargetPortSelector = this.Get<ComboBox>("TargetPortSelector");
        }
        
        Launchpad _launchpad;

        Popout Popout;
        ComboBox Rotation, InputFormatSelector, TargetPortSelector;

        public void UpdatePorts() {
            List<Launchpad> ports = (from i in MIDI.Devices where i.Available && i.Type != Launchpad.LaunchpadType.Unknown && i.GetType() != typeof(AbletonLaunchpad) select i).ToList();

            Launchpad target = null;

            if (_launchpad is AbletonLaunchpad abletonLaunchpad) {
                target = abletonLaunchpad.Target;
                if (target != null && (!target.Available || target.Type == Launchpad.LaunchpadType.Unknown)) ports.Add(target);
            }

            ports.Add(MIDI.NoOutput);

            TargetPortSelector.Items = ports;
            TargetPortSelector.SelectedIndex = -1;
            TargetPortSelector.SelectedItem = target;
        }

        private void HandlePorts() => Dispatcher.UIThread.InvokeAsync((Action)UpdatePorts);

        public LaunchpadInfo(Launchpad launchpad) {
            InitializeComponent();
            
            _launchpad = launchpad;

            this.Get<TextBlock>("Name").Text = _launchpad.Name.Trim();

            Rotation.SelectedIndex = (int)_launchpad.Rotation;
            InputFormatSelector.SelectedIndex = (int)_launchpad.InputFormat;

            if (_launchpad.GetType() != typeof(Launchpad)) {
                Rotation.IsEnabled = InputFormatSelector.IsEnabled = false;
                Rotation.Opacity = Rotation.Width = InputFormatSelector.Opacity = InputFormatSelector.Width = 0;
            }

            if (_launchpad.GetType() == typeof(VirtualLaunchpad)) {
                Popout.IsEnabled = false;
                Popout.Opacity = Popout.Width = 0;
            }

            if (_launchpad.GetType() == typeof(AbletonLaunchpad)) {
                TargetPortSelector.IsEnabled = true;
                TargetPortSelector.Opacity = 1;

                UpdatePorts();
                MIDI.DevicesUpdated += HandlePorts;
            }
        }

        private void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            if (_launchpad.GetType() == typeof(AbletonLaunchpad))
                MIDI.DevicesUpdated -= HandlePorts;
            
            _launchpad = null;
        }

        private void Launchpad_Popout() => LaunchpadWindow.Create(_launchpad, (Window)this.GetVisualRoot());

        private void Rotation_Changed(object sender, SelectionChangedEventArgs e) => _launchpad.Rotation = (Launchpad.RotationType)Rotation.SelectedIndex;

        private void InputFormat_Changed(object sender, SelectionChangedEventArgs e) => _launchpad.InputFormat = (Launchpad.InputType)InputFormatSelector.SelectedIndex;

        private void TargetPort_Changed(object sender, SelectionChangedEventArgs e) {
            Launchpad selected = (Launchpad)TargetPortSelector.SelectedItem;

            if (_launchpad is AbletonLaunchpad abletonLaunchpad && selected != null && abletonLaunchpad.Target != selected)
                abletonLaunchpad.Target = selected;
        }
    }
}
