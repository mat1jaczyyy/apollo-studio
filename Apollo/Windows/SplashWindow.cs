using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Apollo.Binary;
using Apollo.Components;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class SplashWindow: Window {
        static Image SplashImage = (Image)Application.Current.Styles.FindResource("SplashImage");

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Root = this.Get<Grid>("Root");
            Recents = this.Get<StackPanel>("Recents");
        }

        Grid Root;
        StackPanel Recents;

        void UpdateTopmost(bool value) => Topmost = value;

        public SplashWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            this.Get<PreferencesButton>("PreferencesButton").HoleFill = Background;
            
            Root.Children.Add(SplashImage);

            for (int i = 0; i < Math.Min(8, Preferences.Recents.Count); i++) {
                RecentProjectInfo viewer = new RecentProjectInfo(Preferences.Recents[i]);
                viewer.Opened += ReadFile;

                Recents.Children.Add(viewer);
            }
        }

        async void Loaded(object sender, EventArgs e) {
            Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));

            if (Launchpad.CFWIncompatible == CFWIncompatibleState.Show) {
                await MessageWindow.Create(
                    "One or more connected Launchpad Pros are running an older version of the\n" + 
                    "performance-optimized custom firmware which is not compatible with\n" +
                    "Apollo Studio.\n\n" +
                    "Update these to the latest version of the firmware or switch back to stock\n" +
                    "firmware to use them with Apollo Studio.",
                    null, this  
                );
                Launchpad.CFWIncompatible = CFWIncompatibleState.Done;
            }

            if (Program.Args?.Length > 0)
                ReadFile(Program.Args[0]);
            
            Program.Args = null;
        }
        
        void Unloaded(object sender, EventArgs e) {
            Root.Children.Remove(SplashImage);
            
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;

            this.Content = null;

            Program.WindowClose(this);
        }

        public void New(object sender, RoutedEventArgs e) {
            Program.Project?.Dispose();
            Program.Project = new Project();
            ProjectWindow.Create(this);
            Close();
        }

        public async void ReadFile(string path) {
            Project loaded;

            try {
                using (FileStream file = File.Open(path, FileMode.Open, FileAccess.Read))
                    loaded = await Decoder.Decode(file, typeof(Project));

            } catch {
                await MessageWindow.Create(
                    $"An error occurred while reading the file.\n\n" +
                    "You may not have sufficient privileges to read from the destination folder, or\n" +
                    "the file you're attempting to read is invalid.",
                    null, this
                );

                return;
            }

            loaded.FilePath = path;
            loaded.Undo.SavePosition();
            Preferences.RecentsAdd(path);

            Program.Project = loaded;
            ProjectWindow.Create(this);

            Close();
        }

        public async void Open(object sender, RoutedEventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog() {
                AllowMultiple = false,
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                        Extensions = new List<string>() {
                            "approj"
                        },
                        Name = "Apollo Project"
                    }
                },
                Title = "Open Project"
            };

            string[] result = await ofd.ShowAsync(this);

            if (result.Length > 0)
                ReadFile(result[0]);
        }

        void URL(string url) => Process.Start(new ProcessStartInfo() {
            FileName = url,
            UseShellExecute = true
        });

        public void Docs(object sender, RoutedEventArgs e)
            => URL("https://github.com/mat1jaczyyy/apollo-studio/wiki");

        public void Tutorials(object sender, RoutedEventArgs e) {}

        public void Bug(object sender, RoutedEventArgs e)
            => URL("https://github.com/mat1jaczyyy/apollo-studio/issues/new?assignees=mat1jaczyyy&labels=bug&template=bug_report.md&title=");

        public void Feature(object sender, RoutedEventArgs e)
            => URL("https://github.com/mat1jaczyyy/apollo-studio/issues/new?assignees=mat1jaczyyy&labels=enhancement&template=feature_request.md&title=");

        public void Discord(object sender, RoutedEventArgs e) {}

        void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
    }
}
