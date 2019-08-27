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

using Humanizer;
using Humanizer.Localisation;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Structures;
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

            FadeSmoothness = this.Get<HorizontalDial>("FadeSmoothness");

            CopyPreviousFrame = this.Get<CheckBox>("CopyPreviousFrame");
            CaptureLaunchpad = this.Get<CheckBox>("CaptureLaunchpad");
            EnableGestures = this.Get<CheckBox>("EnableGestures");

            Monochrome = this.Get<RadioButton>("Monochrome");
            NovationPalette = this.Get<RadioButton>("NovationPalette");
            CustomPalette = this.Get<RadioButton>("CustomPalette");

            ThemeHeader = this.Get<TextBlock>("ThemeHeader");
            Dark = this.Get<RadioButton>("Dark");
            Light = this.Get<RadioButton>("Light");

            Backup = this.Get<CheckBox>("Backup");
            Autosave = this.Get<CheckBox>("Autosave");

            UndoLimit = this.Get<CheckBox>("UndoLimit");

            DiscordPresence = this.Get<CheckBox>("DiscordPresence");
            DiscordFilename = this.Get<CheckBox>("DiscordFilename");

            CheckForUpdates = this.Get<CheckBox>("CheckForUpdates");

            Contents = this.Get<StackPanel>("Contents").Children;

            CurrentSession = this.Get<TextBlock>("CurrentSession");
            AllTime = this.Get<TextBlock>("AllTime");

            Preview = this.Get<LaunchpadGrid>("Preview");
        }

        CheckBox AlwaysOnTop, CenterTrackContents, AutoCreateKeyFilter, AutoCreatePageFilter, AutoCreatePattern, CopyPreviousFrame, CaptureLaunchpad, EnableGestures, Backup, Autosave, UndoLimit, DiscordPresence, DiscordFilename, CheckForUpdates;
        ComboBox LaunchpadStyle, LaunchpadGridRotation;
        TextBlock ThemeHeader, CurrentSession, AllTime;
        RadioButton Monochrome, NovationPalette, CustomPalette, Dark, Light;
        HorizontalDial FadeSmoothness;
        Controls Contents;
        DispatcherTimer Timer;
        LaunchpadGrid Preview;

        Fade fade = new Fade(
            new Time(false),
            colors: new List<Color>() {
                new Color(1, 0, 0),
                new Color(63, 0, 0),
                new Color(63, 63, 0),
                new Color(0, 63, 0),
                new Color(0, 63, 63),
                new Color(0, 0, 63),
                new Color(63, 0, 63),
                new Color(63, 0, 0),
                new Color(1, 0, 0)
            },
            positions: new List<double>() {
                0, 0.125, 0.25, 0.375, 0.5, 0.625, 0.75, 0.875, 1
            }
        );

        void UpdateTopmost(bool value) => Topmost = value;

        void UpdatePorts() {
            for (int i = Contents.Count - 2; i >= 0; i--) Contents.RemoveAt(i);

            foreach (LaunchpadInfo control in (from i in MIDI.Devices where i.Available && i.Type != LaunchpadType.Unknown select new LaunchpadInfo(i)))
                Contents.Insert(Contents.Count - 1, control);
        }

        void HandlePorts() => Dispatcher.UIThread.InvokeAsync((Action)UpdatePorts);

        void UpdateTime(object sender, EventArgs e) {
            CurrentSession.Text = $"Current session: {Program.TimeSpent.Elapsed.Humanize(minUnit: TimeUnit.Second, maxUnit: TimeUnit.Hour)}";

            if (Preferences.Time >= (long)TimeSpan.MaxValue.TotalSeconds) Preferences.BaseTime = 0;

            AllTime.Text = $"All time: {Preferences.Time.Seconds().Humanize(minUnit: TimeUnit.Second, maxUnit: TimeUnit.Hour)}";
        }

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

            CheckForUpdates.IsChecked = Preferences.CheckForUpdates;

            UpdateTime(null, EventArgs.Empty);
            Timer = new DispatcherTimer() {
                Interval = new TimeSpan(0, 0, 1)
            };
            Timer.Tick += UpdateTime;
            Timer.Start();

            UpdatePorts();
            MIDI.DevicesUpdated += HandlePorts;

            fade.MIDIExit = FadeExit;
        }

        void Loaded(object sender, EventArgs e) => Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));

        void Unloaded(object sender, EventArgs e) {
            Preferences.Window = null;

            Timer.Stop();
            Timer.Tick -= UpdateTime;

            MIDI.DevicesUpdated -= HandlePorts;

            Preferences.AlwaysOnTopChanged -= UpdateTopmost;

            this.Content = null;
            
            Preview = null;
            fade.Dispose();
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

        void FadeSmoothness_Changed(double value, double? old) => Preferences.FadeSmoothness = value / 100;

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

        void SetTheme(Themes theme) {
            ThemeHeader.Text = (Preferences.Theme != theme)? "You must restart\nApollo Studio to\napply this change." : "";
            Preferences.Theme = theme;
        }

        void Dark_Changed(object sender, EventArgs e) => SetTheme(Themes.Dark);

        void Light_Changed(object sender, EventArgs e) => SetTheme(Themes.Light);

        void Backup_Changed(object sender, EventArgs e) => Preferences.Backup = Backup.IsChecked.Value;

        void Autosave_Changed(object sender, EventArgs e) => Preferences.Autosave = Autosave.IsChecked.Value;

        void ClearRecentProjects(object sender, RoutedEventArgs e) => Preferences.RecentsClear();

        void UndoLimit_Changed(object sender, EventArgs e) => Preferences.UndoLimit = UndoLimit.IsChecked.Value;

        void DiscordPresence_Changed(object sender, EventArgs e) => Preferences.DiscordPresence = DiscordPresence.IsChecked.Value;

        void DiscordFilename_Changed(object sender, EventArgs e) => Preferences.DiscordFilename = DiscordFilename.IsChecked.Value;

        void CheckForUpdates_Changed(object sender, EventArgs e) => Preferences.CheckForUpdates = DiscordFilename.IsChecked.Value;

        void OpenCrashesFolder(object sender, RoutedEventArgs e) {
            if (!Directory.Exists(Program.UserPath)) Directory.CreateDirectory(Program.UserPath);
            if (!Directory.Exists(Program.CrashDir)) Directory.CreateDirectory(Program.CrashDir);

            Program.URL(Program.CrashDir);
        }

        void LocateApolloConnector(object sender, RoutedEventArgs e) {
            string m4l = Program.GetBaseFolder("M4L");

            if (!Directory.Exists(m4l)) Directory.CreateDirectory(m4l);

            Program.URL(m4l);
        }

        void Launchpad_Add() {
            LaunchpadWindow.Create(MIDI.ConnectVirtual(), this);
            MIDI.Update();
        }

        void Preview_Pressed(int index) =>
            fade.MIDIEnter(new Signal(null, null, (byte)LaunchpadGrid.GridToSignal(index), new Color(63)));

        void FadeExit(Signal n) => Dispatcher.UIThread.InvokeAsync(() => {
            Preview?.SetColor(LaunchpadGrid.SignalToGrid(n.Index), n.Color.ToScreenBrush());
        });

        async void Window_KeyDown(object sender, KeyEventArgs e) {
            if (!Program.WindowKey(this, e) && Program.Project != null && !await Program.Project.HandleKey(this, e))
                Program.Project?.Undo.HandleKey(e);
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