using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Humanizer;

using Apollo.Core;
using Apollo.Helpers;

namespace Apollo.Windows {
    public class UpdateWindow: Window {
        static Image UpdateImage = (Image)Apollo.Core.App.FindResource("UpdateImage");

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Root = this.Get<Grid>("Root");
            State = this.Get<TextBlock>("State");
            DownloadProgress = this.Get<ProgressBar>("DownloadProgress");
            CloseButton = this.Get<Button>("CloseButton");
        }

        Grid Root;
        TextBlock State;
        ProgressBar DownloadProgress;
        Button CloseButton;

        Stopwatch time = new Stopwatch();
        bool exiting = false;

        ZipArchiveEntry GetZipFolder(ZipArchive zip, string folder) => zip.Entries.First(i => {
            string[] path = i.FullName.Split('/');
            return path.Length == 3 && path[1] == folder && path[2] == "";
        });

        void ExtractWin(ZipArchiveEntry directory, string path) {
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

        void ExtractMac(string directory, string path) {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            
            Directory.Move(directory, path);
        }

        public UpdateWindow() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                throw new InvalidOperationException("Auto-updating is not supported on Linux");

            InitializeComponent();
            
            Root.Children.Add(UpdateImage);
        }

        protected virtual async Task<byte[]> Download() {
            var asset = await Github.LatestDownload();
            if (asset == null || !Uri.TryCreate(asset.BrowserDownloadUrl, UriKind.Absolute, out var uri))
                throw new IOException("No compatible update is available for this installation.");
            using var downloader = new WebClient();
            downloader.DownloadProgressChanged += Progress;
            return await downloader.DownloadDataTaskAsync(uri);
        }

        async void HandleLoaded(object sender, EventArgs e) {
            Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));

            State.Text = "Downloading...";

            try {
                var result = await Download();
                PrepareUpdate(result);
                Program.LaunchUpdater = true;
                exiting = true;
                App.Shutdown();
            } catch (Exception error) {
                State.Text = "Update failed: " + error.Message;
                DownloadProgress.IsVisible = false;
                CloseButton.IsVisible = true;
                exiting = true;
            }
        }
        
        void HandleUnloaded(object sender, WindowClosingEventArgs e) {
            if (!exiting) {
                e.Cancel = true;
                return;
            }

            Root.Children.Remove(UpdateImage);

            this.Content = null;
        }

        void Progress(object sender, DownloadProgressChangedEventArgs e) {
            if (!time.IsRunning) time.Start();

            DownloadProgress.Value = e.ProgressPercentage;
            State.Text = $"Downloading... ({(e.BytesReceived * 1000.0 / (time.ElapsedMilliseconds + 50)).Bytes().Humanize("#.#")}/s)";
        }

        internal static void ValidateArchive(ZipArchive zip) {
            foreach (var entry in zip.Entries) {
                string name = entry.FullName;
                if (name.StartsWith('/') || name.Contains('\\') || name.Contains(':') || name.Split('/').Contains("..")
                    || ((entry.ExternalAttributes >> 16) & 0xf000) == 0xa000)
                    throw new InvalidDataException("The update archive contains an unsafe path or symbolic link.");
            }
        }

        void PrepareUpdate(byte[] result) {
            // Check the entire archive before clearing any existing staging folders.
            using var zip = new ZipArchive(new MemoryStream(result));
            ValidateArchive(zip);
            string updatepath = Program.GetBaseFolder("Update");
            string temppath = Program.GetBaseFolder("Temp");
            string tempm4lpath = Program.GetBaseFolder("TempM4L");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                ExtractWin(GetZipFolder(zip, "Update"), updatepath);
                ExtractWin(GetZipFolder(zip, "Apollo"), temppath);
                ExtractWin(GetZipFolder(zip, "M4L"), tempm4lpath);

            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                string zippath = Program.GetBaseFolder("Zip");

                if (Directory.Exists(zippath))
                    Directory.Delete(zippath, true);
                
                Directory.CreateDirectory(zippath);

                string zipfile = Path.Combine(zippath, "update.zip");
            
                File.WriteAllBytes(zipfile, result);

                var start = new ProcessStartInfo("/usr/bin/ditto") { UseShellExecute = false };
                foreach (var argument in new[] { "-x", "-k", "--sequesterRsrc", "--rsrc", zipfile, zippath })
                    start.ArgumentList.Add(argument);
                using var process = Process.Start(start);
                process.WaitForExit();
                if (process.ExitCode != 0) throw new IOException("Could not unpack the update archive.");

                string foldername = Directory.GetDirectories(zippath)[0];

                ExtractMac(Path.Combine(zippath, foldername, "Update"), updatepath);
                ExtractMac(Path.Combine(zippath, foldername, "Apollo"), temppath);
                ExtractMac(Path.Combine(zippath, foldername, "M4L"), tempm4lpath);

                Directory.Delete(zippath, true);
            }

        }

        void CloseUpdate(object sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
        
        void Minimize() => WindowState = WindowState.Minimized;

        void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag(e);
        
        public static void Create(Window owner) {
            UpdateWindow window = new UpdateWindow();
                
            if (owner == null || owner.WindowState == WindowState.Minimized) 
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            else
                window.Owner = owner;

            window.Show();
            window.Owner = null;

            window.Topmost = true;
            window.Topmost = Preferences.AlwaysOnTop;
        }
    }
}
