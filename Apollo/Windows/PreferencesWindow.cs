using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using Apollo.Components;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class PreferencesWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        CheckBox AlwaysOnTop, CenterTrackContents, AutoCreateKeyFilter, AutoCreatePageFilter, CopyPreviousFrame, CaptureLaunchpad, EnableGestures, DiscordPresence, DiscordFilename;
        Slider FadeSmoothness;
        Controls Contents;

        private void UpdateTopmost(bool value) => Topmost = value;

        private void UpdatePorts() {
            for (int i = Contents.Count - 2; i >= 0; i--) Contents.RemoveAt(i);

            foreach (LaunchpadInfo control in (from i in MIDI.Devices where i.Available && i.Type != Launchpad.LaunchpadType.Unknown select new LaunchpadInfo(i)))
                Contents.Insert(Contents.Count - 1, control);
        }

        private void HandlePorts() => Dispatcher.UIThread.InvokeAsync((Action)UpdatePorts);

        public PreferencesWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            Preferences.Window = this;

            this.Get<TextBlock>("Version").Text += Program.Version;

            AlwaysOnTop = this.Get<CheckBox>("AlwaysOnTop");
            AlwaysOnTop.IsChecked = Preferences.AlwaysOnTop;

            CenterTrackContents = this.Get<CheckBox>("CenterTrackContents");
            CenterTrackContents.IsChecked = Preferences.CenterTrackContents;

            AutoCreateKeyFilter = this.Get<CheckBox>("AutoCreateKeyFilter");
            AutoCreateKeyFilter.IsChecked = Preferences.AutoCreateKeyFilter;

            AutoCreatePageFilter = this.Get<CheckBox>("AutoCreatePageFilter");
            AutoCreatePageFilter.IsChecked = Preferences.AutoCreatePageFilter;

            FadeSmoothness = this.Get<Slider>("FadeSmoothness");
            FadeSmoothness.Value = Preferences.FadeSmoothnessSlider;
            FadeSmoothness.GetObservable(Slider.ValueProperty).Subscribe(FadeSmoothness_Changed);

            CopyPreviousFrame = this.Get<CheckBox>("CopyPreviousFrame");
            CopyPreviousFrame.IsChecked = Preferences.CopyPreviousFrame;

            CaptureLaunchpad = this.Get<CheckBox>("CaptureLaunchpad");
            CaptureLaunchpad.IsChecked = Preferences.CaptureLaunchpad;

            EnableGestures = this.Get<CheckBox>("EnableGestures");
            EnableGestures.IsChecked = Preferences.EnableGestures;

            DiscordPresence = this.Get<CheckBox>("DiscordPresence");
            DiscordPresence.IsChecked = Preferences.DiscordPresence;

            DiscordFilename = this.Get<CheckBox>("DiscordFilename");
            DiscordFilename.IsChecked = Preferences.DiscordFilename;

            Contents = this.Get<StackPanel>("Contents").Children;
            UpdatePorts();
            MIDI.DevicesUpdated += HandlePorts;
        }

        private void Loaded(object sender, EventArgs e) => Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));

        private void Unloaded(object sender, EventArgs e) {
            Preferences.Window = null;

            Preferences.AlwaysOnTopChanged -= UpdateTopmost;
        }

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();

        private void AlwaysOnTop_Changed(object sender, EventArgs e) {
            Preferences.AlwaysOnTop = AlwaysOnTop.IsChecked.Value;
            Activate();
        }

        private void CenterTrackContents_Changed(object sender, EventArgs e) => Preferences.CenterTrackContents = CenterTrackContents.IsChecked.Value;

        private void AutoCreateKeyFilter_Changed(object sender, EventArgs e) => Preferences.AutoCreateKeyFilter = AutoCreateKeyFilter.IsChecked.Value;

        private void AutoCreatePageFilter_Changed(object sender, EventArgs e) => Preferences.AutoCreatePageFilter = AutoCreatePageFilter.IsChecked.Value;

        private void FadeSmoothness_Changed(double value) => Preferences.FadeSmoothness = value;

        private void CaptureLaunchpad_Changed(object sender, EventArgs e) => Preferences.CaptureLaunchpad = CaptureLaunchpad.IsChecked.Value;

        private void CopyPreviousFrame_Changed(object sender, EventArgs e) => Preferences.CopyPreviousFrame = CopyPreviousFrame.IsChecked.Value;

        private void EnableGestures_Changed(object sender, EventArgs e) => Preferences.EnableGestures = EnableGestures.IsChecked.Value;

        private void ClearColorHistory(object sender, RoutedEventArgs e) => ColorHistory.Clear();

        private void DiscordPresence_Changed(object sender, EventArgs e) => Preferences.DiscordPresence = DiscordPresence.IsChecked.Value;

        private void DiscordFilename_Changed(object sender, EventArgs e) => Preferences.DiscordFilename = DiscordFilename.IsChecked.Value;

        private void Launchpad_Add() {
            LaunchpadWindow.Create(MIDI.Connect(), this);
            MIDI.Update();
        }

        public static void Create(Window owner) {
            if (Preferences.Window == null) {
                Preferences.Window = new PreferencesWindow() {Owner = owner};
                Preferences.Window.Show();
                Preferences.Window.Owner = null;
            } else {
                Preferences.Window.WindowState = WindowState.Normal;
                Preferences.Window.Activate();
            }
        }
    }
}