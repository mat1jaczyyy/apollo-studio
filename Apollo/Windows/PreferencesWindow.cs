using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using Apollo.Core;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class PreferencesWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        CheckBox AlwaysOnTop, CenterTrackContents, AutoCreateKeyFilter, AutoCreatePageFilter, CopyPreviousFrame;
        Slider FadeSmoothness;
        Controls Contents;

        private void UpdateTopmost(bool value) => Topmost = value;

        private void UpdatePorts() {
            Contents.Clear();
            foreach (LaunchpadInfo control in (from i in MIDI.Devices where i.Available select new LaunchpadInfo(i))) {
                Contents.Add(control);
            }
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

            AlwaysOnTop = this.Get<CheckBox>("AlwaysOnTop");
            AlwaysOnTop.IsChecked = Preferences.AlwaysOnTop;

            CenterTrackContents = this.Get<CheckBox>("CenterTrackContents");
            CenterTrackContents.IsChecked = Preferences.CenterTrackContents;

            AutoCreateKeyFilter = this.Get<CheckBox>("AutoCreateKeyFilter");
            AutoCreateKeyFilter.IsChecked = Preferences.AutoCreateKeyFilter;

            AutoCreatePageFilter = this.Get<CheckBox>("AutoCreatePageFilter");
            AutoCreatePageFilter.IsChecked = Preferences.AutoCreatePageFilter;

            FadeSmoothness = this.Get<Slider>("FadeSmoothness");
            FadeSmoothness.Value = Preferences.FadeSmoothness;
            FadeSmoothness.GetObservable(Slider.ValueProperty).Subscribe(FadeSmoothness_Changed);

            CopyPreviousFrame = this.Get<CheckBox>("CopyPreviousFrame");
            CopyPreviousFrame.IsChecked = Preferences.CopyPreviousFrame;

            Contents = this.Get<StackPanel>("Contents").Children;
            UpdatePorts();
            MIDI.DevicesUpdated += HandlePorts;
        }

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

        private void CopyPreviousFrame_Changed(object sender, EventArgs e) => Preferences.CopyPreviousFrame = CopyPreviousFrame.IsChecked.Value;

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