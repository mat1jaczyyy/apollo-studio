using System;
using System.Collections.Generic;
using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;

using Apollo.Binary;
using Apollo.Components;
using Apollo.Core;
using Apollo.Elements;

namespace Apollo.Windows {
    public class SplashWindow: Window {
        private static Image SplashImage = (Image)Application.Current.Styles.FindResource("SplashImage");

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        Grid Root;

        private void UpdateTopmost(bool value) => Topmost = value;

        public SplashWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            this.Get<PreferencesButton>("PreferencesButton").Fill = Background;
            
            Root = this.Get<Grid>("Root");
            Root.Children.Add(SplashImage);
        }

        private void Loaded(object sender, EventArgs e) {
            Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));

            if (Program.Args?.Length > 0)
                ReadFile(Program.Args[0]);
            
            Program.Args = null;
        }
        
        private void Unloaded(object sender, EventArgs e) {
            Root.Children.Remove(SplashImage);
            
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;

            Program.WindowClose(this);
        }

        public void New(object sender, RoutedEventArgs e) {
            Program.Project?.Dispose();
            Program.Project = new Project();
            ProjectWindow.Create(this);
            Close();
        }

        public void ReadFile(string path) {
            Project loaded;

            using (FileStream file = File.Open(path, FileMode.Open, FileAccess.Read))
                try {
                    loaded = Decoder.Decode(file, typeof(Project));

                } catch {
                    ErrorWindow.Create(
                        $"An error occurred while reading the file.\n\n" +
                        "You may not have sufficient privileges to read from the destination folder, or the file you're attempting to read is invalid.",
                        this
                    );

                    return;
                }

            loaded.FilePath = path;
            loaded.Undo.SavePosition();

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

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
    }
}
