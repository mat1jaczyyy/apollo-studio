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
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void UpdateTopmost(bool value) => Topmost = value;

        public SplashWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            this.Get<PreferencesButton>("PreferencesButton").Fill = Background;
            this.Get<Image>("SplashImage").Source = new Bitmap((string)Application.Current.Styles.FindResource("SplashImage"));
        }

        private void Loaded(object sender, EventArgs e) => Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));
        
        private void Unloaded(object sender, EventArgs e) {
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;

            Program.WindowClose(this);
        }

        public void New(object sender, RoutedEventArgs e) {
            Program.Project?.Dispose();
            Program.Project = new Project();
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
            if (result.Length > 0) {
                Project loaded;

                using (FileStream file = File.Open(result[0], FileMode.Open, FileAccess.Read))
                    loaded = Decoder.Decode(file, typeof(Project));
                
                loaded.FilePath = result[0];
                loaded.Undo.SavePosition();

                if (loaded != null) {
                    Program.Project = loaded;
                    ProjectWindow.Create(this);
                    Close();
                }
            }
        }

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
    }
}
