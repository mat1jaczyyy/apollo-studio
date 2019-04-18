using System;
using System.Collections.Generic;
using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

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
        }
        
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
                Project loaded = Project.Decode(File.ReadAllText(result[0]), result[0]);

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
