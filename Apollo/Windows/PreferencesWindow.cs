using System;
using System.Collections.Generic;
using System.IO;
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
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class PreferencesWindow: Window {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            AlwaysOnTop = this.Get<CheckBox>("AlwaysOnTop");
            CenterTrackContents = this.Get<CheckBox>("CenterTrackContents");
            LaunchpadStyle = this.Get<ComboBox>("LaunchpadStyle");
            LaunchpadGridRotation = this.Get<ComboBox>("LaunchpadGridRotation");

            AutoCreateKeyFilter = this.Get<CheckBox>("AutoCreateKeyFilter");
            AutoCreatePageFilter = this.Get<CheckBox>("AutoCreatePageFilter");
            AutoCreatePattern = this.Get<CheckBox>("AutoCreatePattern");

            FadeSmoothness = this.Get<Slider>("FadeSmoothness");

            CopyPreviousFrame = this.Get<CheckBox>("CopyPreviousFrame");
            CaptureLaunchpad = this.Get<CheckBox>("CaptureLaunchpad");
            EnableGestures = this.Get<CheckBox>("EnableGestures");

            Monochrome = this.Get<RadioButton>("Monochrome");
            NovationPalette = this.Get<RadioButton>("NovationPalette");
            CustomPalette = this.Get<RadioButton>("CustomPalette");

            Dark = this.Get<RadioButton>("Dark");
            Light = this.Get<RadioButton>("Light");

            Backup = this.Get<CheckBox>("Backup");
            Autosave = this.Get<CheckBox>("Autosave");

            UndoLimit = this.Get<CheckBox>("UndoLimit");

            DiscordPresence = this.Get<CheckBox>("DiscordPresence");
            DiscordFilename = this.Get<CheckBox>("DiscordFilename");

            Contents = this.Get<StackPanel>("Contents").Children;
        }

        CheckBox AlwaysOnTop, CenterTrackContents, AutoCreateKeyFilter, AutoCreatePageFilter, AutoCreatePattern, CopyPreviousFrame, CaptureLaunchpad, EnableGestures, Backup, Autosave, UndoLimit, DiscordPresence, DiscordFilename;
        ComboBox LaunchpadStyle, LaunchpadGridRotation;
        RadioButton Monochrome, NovationPalette, CustomPalette, Dark, Light;
        Slider FadeSmoothness;
        Controls Contents;

        void UpdateTopmost(bool value) => Topmost = value;

        void UpdatePorts() {
            for (int i = Contents.Count - 2; i >= 0; i--) Contents.RemoveAt(i);

            foreach (LaunchpadInfo control in (from i in MIDI.Devices where i.Available && i.Type != LaunchpadType.Unknown select new LaunchpadInfo(i)))
                Contents.Insert(Contents.Count - 1, control);
        }

        void HandlePorts() => Dispatcher.UIThread.InvokeAsync((Action)UpdatePorts);

        public PreferencesWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            Preferences.Window = this;

            this.Get<TextBlock>("Version").Text += Program.Version;

            AlwaysOnTop.IsChecked = Preferences.AlwaysOnTop;
            CenterTrackContents.IsChecked = Preferences.CenterTrackContents;
            LaunchpadStyle.SelectedIndex = (int)Preferences.LaunchpadStyle;
            LaunchpadGridRotation.SelectedIndex = Convert.ToInt32(Preferences.LaunchpadGridRotation);

            AutoCreateKeyFilter.IsChecked = Preferences.AutoCreateKeyFilter;
            AutoCreatePageFilter.IsChecked = Preferences.AutoCreatePageFilter;
            AutoCreatePattern.IsChecked = Preferences.AutoCreatePattern;

            FadeSmoothness.Value = Preferences.FadeSmoothnessSlider;
            FadeSmoothness.GetObservable(Slider.ValueProperty).Subscribe(FadeSmoothness_Changed);

            CopyPreviousFrame.IsChecked = Preferences.CopyPreviousFrame;
            CaptureLaunchpad.IsChecked = Preferences.CaptureLaunchpad;
            EnableGestures.IsChecked = Preferences.EnableGestures;

            Monochrome.IsChecked = Preferences.ImportPalette == Palettes.Monochrome;
            NovationPalette.IsChecked = Preferences.ImportPalette == Palettes.NovationPalette;
            CustomPalette.Content = $"Custom Retina Palette - {Preferences.PaletteName}";
            CustomPalette.IsChecked = Preferences.ImportPalette == Palettes.CustomPalette;

            Dark.IsChecked = Preferences.Theme == Themes.Dark;
            Light.IsChecked = Preferences.Theme == Themes.Light;

            Backup.IsChecked = Preferences.Backup;
            Autosave.IsChecked = Preferences.Autosave;

            UndoLimit.IsChecked = Preferences.UndoLimit;

            DiscordPresence.IsChecked = Preferences.DiscordPresence;
            DiscordFilename.IsChecked = Preferences.DiscordFilename;

            UpdatePorts();
            MIDI.DevicesUpdated += HandlePorts;
        }

        void Loaded(object sender, EventArgs e) => Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));

        void Unloaded(object sender, EventArgs e) {
            Preferences.Window = null;

            MIDI.DevicesUpdated -= HandlePorts;

            Preferences.AlwaysOnTopChanged -= UpdateTopmost;

            this.Content = null;
        }

        void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();

        void AlwaysOnTop_Changed(object sender, EventArgs e) {
            Preferences.AlwaysOnTop = AlwaysOnTop.IsChecked.Value;
            Activate();
        }

        void CenterTrackContents_Changed(object sender, EventArgs e) => Preferences.CenterTrackContents = CenterTrackContents.IsChecked.Value;

        void LaunchpadStyle_Changed(object sender, EventArgs e) => Preferences.LaunchpadStyle = (LaunchpadStyles)LaunchpadStyle.SelectedIndex;

        void LaunchpadGridRotation_Changed(object sender, EventArgs e) => Preferences.LaunchpadGridRotation = LaunchpadGridRotation.SelectedIndex > 0;

        void AutoCreateKeyFilter_Changed(object sender, EventArgs e) => Preferences.AutoCreateKeyFilter = AutoCreateKeyFilter.IsChecked.Value;

        void AutoCreatePageFilter_Changed(object sender, EventArgs e) => Preferences.AutoCreatePageFilter = AutoCreatePageFilter.IsChecked.Value;

        void AutoCreatePattern_Changed(object sender, EventArgs e) => Preferences.AutoCreatePattern = AutoCreatePattern.IsChecked.Value;

        void FadeSmoothness_Changed(double value) => Preferences.FadeSmoothness = value;

        void CaptureLaunchpad_Changed(object sender, EventArgs e) => Preferences.CaptureLaunchpad = CaptureLaunchpad.IsChecked.Value;

        void CopyPreviousFrame_Changed(object sender, EventArgs e) => Preferences.CopyPreviousFrame = CopyPreviousFrame.IsChecked.Value;

        void EnableGestures_Changed(object sender, EventArgs e) => Preferences.EnableGestures = EnableGestures.IsChecked.Value;

        void ClearColorHistory(object sender, RoutedEventArgs e) => ColorHistory.Clear();

        void Monochrome_Changed(object sender, EventArgs e) => Preferences.ImportPalette = Palettes.Monochrome;

        void NovationPalette_Changed(object sender, EventArgs e) => Preferences.ImportPalette = Palettes.NovationPalette;

        void CustomPalette_Changed(object sender, EventArgs e) => Preferences.ImportPalette = Palettes.CustomPalette;

        async void BrowseCustomPalette(object sender, RoutedEventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog() {
                AllowMultiple = false,
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                        Extensions = new List<string>() {
                            "*"
                        },
                        Name = "Retina Palette File"
                    }
                },
                Title = "Select Retina Palette"
            };

            string[] result = await ofd.ShowAsync(this);

            if (result.Length > 0) {
                Palette loaded;

                using (FileStream file = File.Open(result[0], FileMode.Open, FileAccess.Read))
                    loaded = Palette.Decode(file);
                
                if (loaded != null) {
                    Preferences.CustomPalette = loaded;
                    CustomPalette.Content = $"Custom Retina Palette - {Preferences.PaletteName = Path.GetFileNameWithoutExtension(result[0])}";
                    CustomPalette.IsChecked = true;
                    Preferences.ImportPalette = Palettes.CustomPalette;
                }
            }
        }

        void Dark_Changed(object sender, EventArgs e) {
            if (Preferences.Theme != Themes.Dark)
                this.Get<TextBlock>("ThemeHeader").Text = "Theme     You must restart Apollo to apply this change.";

            Preferences.Theme = Themes.Dark;
        }

        void Light_Changed(object sender, EventArgs e) {
            if (Preferences.Theme != Themes.Light)
                this.Get<TextBlock>("ThemeHeader").Text = "Theme     You must restart Apollo to apply this change.";
            
            Preferences.Theme = Themes.Light;
        }

        void Backup_Changed(object sender, EventArgs e) => Preferences.Backup = Backup.IsChecked.Value;

        void Autosave_Changed(object sender, EventArgs e) => Preferences.Autosave = Autosave.IsChecked.Value;

        void UndoLimit_Changed(object sender, EventArgs e) => Preferences.UndoLimit = UndoLimit.IsChecked.Value;

        void DiscordPresence_Changed(object sender, EventArgs e) => Preferences.DiscordPresence = DiscordPresence.IsChecked.Value;

        void DiscordFilename_Changed(object sender, EventArgs e) => Preferences.DiscordFilename = DiscordFilename.IsChecked.Value;

        void Launchpad_Add() {
            LaunchpadWindow.Create(MIDI.ConnectVirtual(), this);
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