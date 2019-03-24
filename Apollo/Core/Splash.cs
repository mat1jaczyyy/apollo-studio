using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;

using Apollo.Elements;
using Apollo.Windows;

namespace Apollo.Core {
    public class Splash: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public Splash() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif

            this.Get<Image>("Logo").Source = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("Apollo.Resources.SplashImage.png"));
            Icon = new WindowIcon(Assembly.GetExecutingAssembly().GetManifestResourceStream("Apollo.Resources.WindowIcon.png"));
        }

        public void New_Click(object sender, RoutedEventArgs e) {
            Program.Project?.Dispose();
            Program.Project = new Project();
            new ProjectWindow().Show();
        }

        public async void Open_Click(object sender, RoutedEventArgs e) {
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
                    Program.Project?.Dispose();
                    Program.Project = loaded;
                    new ProjectWindow().Show();
                }
            }
        }
    }
}
