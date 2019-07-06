using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Humanizer;

using Apollo.Core;
using Apollo.Helpers;

namespace Apollo.Windows {
    public class UpdateWindow: Window {
        static Image UpdateImage = (Image)Application.Current.Styles.FindResource("UpdateImage");

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Root = this.Get<Grid>("Root");
            State = this.Get<TextBlock>("State");
            DownloadProgress = this.Get<ProgressBar>("DownloadProgress");
        }

        Grid Root;
        TextBlock State;
        ProgressBar DownloadProgress;

        Stopwatch time = new Stopwatch();

        ZipArchiveEntry GetZipFolder(ZipArchive zip, string folder) => zip.Entries.First(i => {
            string[] path = i.FullName.Split('/');
            return path.Length == 3 && path[1] == folder && path[2] == "";
        });

        void Extract(ZipArchiveEntry directory, string path) {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            
            Directory.CreateDirectory(path);

            foreach (ZipArchiveEntry i in directory.Archive.Entries) {
                if (i.FullName.StartsWith(directory.FullName)) {
                    string name = i.FullName.Replace(directory.FullName, "");

                    if (name.EndsWith("/")) Directory.CreateDirectory(Path.Combine(path, name));
                    else if (name != "") i.ExtractToFile(Path.Combine(path, name));
                }
            }
        }

        public UpdateWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            Root.Children.Add(UpdateImage);
        }

        async void Loaded(object sender, EventArgs e) {
            Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));

            State.Text = "Downloading...";

            WebClient downloader = new WebClient();
            downloader.DownloadProgressChanged += Progress;
            downloader.DownloadDataCompleted += Downloaded;
            downloader.DownloadDataAsync(new Uri((await Github.LatestDownload()).BrowserDownloadUrl));
        }
        
        void Unloaded(object sender, EventArgs e) {
            Root.Children.Remove(UpdateImage);

            this.Content = null;
        }

        void Progress(object sender, DownloadProgressChangedEventArgs e) {
            if (!time.IsRunning) time.Start();

            DownloadProgress.Value = e.ProgressPercentage;
            State.Text = $"Downloading... ({(e.BytesReceived * 1000.0 / (time.ElapsedMilliseconds + 50)).Bytes().Humanize("#.#")}/s)";
        }

        void Downloaded(object sender, AsyncCompletedEventArgs e) {
            ZipArchive zip = new ZipArchive(new MemoryStream(((DownloadDataCompletedEventArgs)e).Result));
            
            Extract(
                GetZipFolder(zip, "Update"),
                Program.GetBaseFolder("Update")
            );

            Extract(
                GetZipFolder(zip, "Apollo"),
                Program.GetBaseFolder("Temp")
            );

            Program.LaunchUpdater = true;

            Application.Current.Exit();
        }

        void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
        
        public static void Create(Window owner) {
            UpdateWindow window = new UpdateWindow() {Owner = owner};
            window.Show();
            window.Owner = null;
        }
    }
}
