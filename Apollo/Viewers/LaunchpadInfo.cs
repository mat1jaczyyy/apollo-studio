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
using Apollo.Enums;
using Apollo.Windows;

namespace Apollo.Viewers {
    public class LaunchpadInfo: UserControl {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Reconnect = this.Get<Reconnect>("Reconnect");
            LockToggle = this.Get<LockToggle>("LockToggle");
            Popout = this.Get<Popout>("Popout");
            Rotation = this.Get<ComboBox>("Rotation");
            InputFormatSelector = this.Get<ComboBox>("InputFormatSelector");
            TargetPortSelector = this.Get<ComboBox>("TargetPortSelector");
        }
        
        Launchpad _launchpad;

        Reconnect Reconnect;
        LockToggle LockToggle;
        Popout Popout;
        ComboBox Rotation, InputFormatSelector, TargetPortSelector;

        public void UpdatePorts() {
            List<Launchpad> ports = MIDI.Devices.Where(i => i.Available && i.Type != LaunchpadType.Unknown && i.GetType() != typeof(AbletonLaunchpad)).ToList();

            Launchpad target = null;

            if (_launchpad is AbletonLaunchpad abletonLaunchpad) {
                target = abletonLaunchpad.Target;
                if (target != null && (!target.Available || target.Type == LaunchpadType.Unknown)) ports.Add(target);
            }

            ports.Add(MIDI.NoOutput);

            TargetPortSelector.Items = ports;
            TargetPortSelector.SelectedIndex = -1;
            TargetPortSelector.SelectedItem = target;
        }

        void HandlePorts() => Dispatcher.UIThread.InvokeAsync((Action)UpdatePorts);

        public LaunchpadInfo() => new InvalidOperationException();

        public LaunchpadInfo(Launchpad launchpad) {
            InitializeComponent();
            
            _launchpad = launchpad;
            _launchpad.Info = this;

            this.Get<TextBlock>("Name").Text = _launchpad.Name.Trim();

            Rotation.SelectedIndex = (int)_launchpad.Rotation;
            InputFormatSelector.SelectedIndex = (int)_launchpad.InputFormat;
            
            Popout.IsVisible = _launchpad.Window == null;

            if (_launchpad.GetType() != typeof(Launchpad)) {
                Reconnect.IsVisible = false;
                Rotation.IsHitTestVisible = InputFormatSelector.IsHitTestVisible = false;
                Rotation.Opacity = InputFormatSelector.Opacity = 0;
            }

            if (_launchpad is VirtualLaunchpad vlp) {
                LockToggle.IsVisible = true;
                LockToggle.SetState(Preferences.VirtualLaunchpads.Contains(vlp.VirtualIndex));
            }

            if (_launchpad is AbletonLaunchpad) {
                TargetPortSelector.IsVisible = true;

                UpdatePorts();
                MIDI.DevicesUpdated += HandlePorts;
            }
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            if (_launchpad.GetType() == typeof(AbletonLaunchpad))
                MIDI.DevicesUpdated -= HandlePorts;
            
            _launchpad.Info = null;
            _launchpad = null;
        }

        public void SetPopout(bool value) => Popout.IsVisible = value;

        void Launchpad_Reconnect() => _launchpad.Reconnect();

        void Launchpad_LockToggle() {
            VirtualLaunchpad lp = (VirtualLaunchpad)_launchpad;

            Preferences.VirtualLaunchpadsToggle(lp.VirtualIndex);

            bool state = Preferences.VirtualLaunchpads.Contains(lp.VirtualIndex);
            LockToggle.SetState(state);

            if (!state && lp.Window == null)
                MIDI.Disconnect(lp);
        }

        void Launchpad_Popout() {
            LaunchpadWindow.Create(_launchpad, (Window)this.GetVisualRoot());
            Popout.IsVisible = false;
        }

        void Rotation_Changed(object sender, SelectionChangedEventArgs e) => _launchpad.Rotation = (RotationType)Rotation.SelectedIndex;

        void InputFormat_Changed(object sender, SelectionChangedEventArgs e) => _launchpad.InputFormat = (InputType)InputFormatSelector.SelectedIndex;

        void TargetPort_Changed(object sender, SelectionChangedEventArgs e) {
            Launchpad selected = (Launchpad)TargetPortSelector.SelectedItem;

            if (_launchpad is AbletonLaunchpad abletonLaunchpad) {
                if (selected != null && abletonLaunchpad.Target != selected && abletonLaunchpad.PatternWindow == null && _launchpad.PatternWindow == null)
                    abletonLaunchpad.Target = selected;
            
                else TargetPortSelector.SelectedItem = abletonLaunchpad.Target;
            }
        }
    }
}
