using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using SelectingItemsControl = Avalonia.Controls.Primitives.SelectingItemsControl;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using Humanizer;

using Apollo.Binary;
using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class SplashWindow: Window {
        static Image SplashImage = (Image)Application.Current.Styles.FindResource("SplashImage");

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Root = this.Get<Grid>("Root");
            CrashPanel = this.Get<Grid>("CrashPanel");

            TabControl = this.Get<TabControl>("TabControl");
            Recents = this.Get<StackPanel>("Recents");

            BlogpostBody = this.Get<TextBlock>("BlogpostBody");
            BlogpostLink = this.Get<TextBlock>("BlogpostLink");

            ReleaseVersion = this.Get<TextBlock>("ReleaseVersion");
            ReleaseBody = this.Get<TextBlock>("ReleaseBody");
            ReleaseLink = this.Get<TextBlock>("ReleaseLink");

            UpdateButton = this.Get<UpdateButton>("UpdateButton");
        }

        Grid Root, CrashPanel;
        TabControl TabControl;
        StackPanel Recents;
        TextBlock BlogpostBody, BlogpostLink, ReleaseVersion, ReleaseBody, ReleaseLink;
        UpdateButton UpdateButton;

        bool openDialog = false;

        void UpdateTopmost(bool value) => Topmost = value;

        async void UpdateBlogpost() {
            Octokit.RepositoryContent latest;

            try {
                latest = await Github.LatestBlogpost();
            } catch {
                BlogpostBody.Text = "Failed to fetch blogpost data from GitHub.";
                return;
            }

            BlogpostBody.Text = $"{latest.Content.Replace("\r", "").Split('\n').First().Replace("# ", "").Replace("#", "")}\n" +
                $" published {DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(Path.GetFileNameWithoutExtension(latest.Name))).Humanize()}";
            
            BlogpostLink.Opacity = 1;
            BlogpostLink.IsHitTestVisible = true;
        }

        async void UpdateRelease() {
            Octokit.Release latest;

            try {
                latest = await Github.LatestRelease();
            } catch {
                ReleaseBody.Text = "Failed to fetch release data from GitHub.";
                return;
            }
            
            ReleaseVersion.Text = $"{latest.Name} - published {latest.PublishedAt.Humanize()}";
            ReleaseBody.Text = String.Join('\n', latest.Body.Replace("\r", "").Split('\n').SkipWhile(i => i.Trim() == "Changes:" || i.Trim() == "").Take(3));
            ReleaseLink.Opacity = 1;
            ReleaseLink.IsHitTestVisible = true;
        }

        public SplashWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            Preferences.RecentsCleared += Clear;

            TabControl.GetObservable(SelectingItemsControl.SelectedIndexProperty).Subscribe(TabChanged);
            
            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);

            this.Get<PreferencesButton>("PreferencesButton").HoleFill = Background;
            
            Root.Children.Add(SplashImage);

            if (Program.HadCrashed)
                if (File.Exists(Program.CrashProject)) {
                    CrashPanel.Opacity = 1;
                    CrashPanel.IsHitTestVisible = true;
                    CrashPanel.ZIndex = 1;

                } else ResolveCrash();
        }

        void Loaded(object sender, EventArgs e) {
            Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));

            Launchpad.DisplayWarnings(this);

            if (App.Args?.Length > 0)
                ReadFile(App.Args[0]);
            
            App.Args = null;

            UpdateBlogpost();
            UpdateRelease();

            if (!Program.HadCrashed) CheckUpdate();
        }
        
        void Unloaded(object sender, CancelEventArgs e) {
            Root.Children.Remove(SplashImage);

            this.RemoveHandler(DragDrop.DropEvent, Drop);
            this.RemoveHandler(DragDrop.DragOverEvent, DragOver);
            
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;
            Preferences.RecentsCleared += Clear;

            this.Content = null;

            App.WindowClosed(this);
        }

        async void CheckUpdate() {
            if (await Github.ShouldUpdate())
                UpdateButton.Enable();
        }

        async void Update() {
            if (IsVisible && !openDialog && await MessageWindow.Create(
                $"A new version of Apollo Studio is available ({(await Github.LatestRelease()).Name} - {(await Github.LatestDownload()).Size.Bytes().Humanize("#.##")}).\n\n" +
                "Do you want to update to the latest version?",
                new string[] { "Yes", "No" }, null
            ) == "Yes") {
                foreach (Window window in App.Windows)
                    if (window.GetType() != typeof(MessageWindow))
                        window.Close();
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) Program.LaunchAdmin = true;
                else UpdateWindow.Create(this);
            }
        }

        void TabChanged(int tab) {
            if (tab == 0) {
                for (int i = 0; i < Preferences.Recents.Count; i++) {
                    RecentProjectInfo viewer = new RecentProjectInfo(Preferences.Recents[i]);
                    viewer.Opened += ReadFile;
                    viewer.Removed += Remove;
                    viewer.Showed += App.URL;

                    Recents.Children.Add(viewer);
                }
            
            } else Recents.Children.Clear();
        }

        void New(object sender, RoutedEventArgs e) {
            Program.Project?.Dispose();
            Program.Project = new Project();

            ProjectWindow.Create(this);
            Close();
        }

        void ReadFile(string path) => ReadFile(path, false);
        async void ReadFile(string path, bool recovery) {
            Project loaded = null;
            Copyable imported = null;

            try {
                try {
                    using (FileStream file = File.Open(path, FileMode.Open, FileAccess.Read))
                        loaded = await Decoder.Decode(file, typeof(Project));

                    if (!recovery) {
                        loaded.FilePath = path;
                        loaded.Undo.SavePosition();
                        Preferences.RecentsAdd(path);
                    }
                    
                    Program.Project?.Dispose();
                    Program.Project = loaded;

                } catch (InvalidDataException) {
                    using (FileStream file = File.Open(path, FileMode.Open, FileAccess.Read))
                        imported = await Decoder.Decode(file, typeof(Copyable));

                    Program.Project?.Dispose();
                    Program.Project = new Project(
                        tracks: (imported.Type == typeof(Track))
                            ? imported.Contents.Cast<Track>().ToList()
                            : new List<Track>() {
                                new Track(new Chain((imported.Type == typeof(Chain))
                                    ? new List<Device>() { new Group(imported.Contents.Cast<Chain>().ToList()) }
                                    : imported.Contents.Cast<Device>().ToList()
                                ))
                            }
                    );
                }

            } catch {
                await MessageWindow.Create(
                    $"An error occurred while reading the file.\n\n" +
                    "You may not have sufficient privileges to read from the destination folder, or\n" +
                    "the file you're attempting to read is invalid.",
                    null, this
                );

                return;
            }
            
            ProjectWindow.Create(this);
            Close();
        }

        async void Open(object sender, RoutedEventArgs e) {
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

            openDialog = true;
            string[] result = await ofd.ShowAsync(this);
            openDialog = false;

            if (result.Length > 0)
                ReadFile(result[0]);
        }

        void Clear() => Dispatcher.UIThread.Post(() => Recents.Children.Clear(), DispatcherPriority.MinValue);

        void Remove(RecentProjectInfo sender, string path) {
            Preferences.RecentsRemove(path);

            Dispatcher.UIThread.Post(() => Recents.Children.Remove(sender), DispatcherPriority.MinValue);
        }

        async void Blogpost(object sender, PointerReleasedEventArgs e)
            => App.URL($"https://apollo.mat1jaczyyy.com/post/{Path.GetFileNameWithoutExtension((await Github.LatestBlogpost()).Name)}");

        async void Release(object sender, PointerReleasedEventArgs e)
            => App.URL((await Github.LatestRelease()).HtmlUrl);

        void ResolveCrash() {
            if (File.Exists(Program.CrashProject))
                File.Delete(Program.CrashProject);
                
            Program.HadCrashed = false;
        }

        void Restore(object sender, RoutedEventArgs e) {
            CrashPanel.Opacity = 0;
            CrashPanel.IsHitTestVisible = false;
            CrashPanel.ZIndex = -1;

            string originalPath = Preferences.CrashPath;

            ReadFile(Program.CrashProject, true);

            if (Program.Project != null) {
                Program.Project.FilePath = originalPath;
                Program.Project.Undo.Clear("Project Restored");
            }

            ResolveCrash();
        }

        void Ignore(object sender, RoutedEventArgs e) {
            CrashPanel.Opacity = 0;
            CrashPanel.IsHitTestVisible = false;
            CrashPanel.ZIndex = -1;

            ResolveCrash();
            CheckUpdate();
        }

        void HandleKey(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            if (App.WindowKey(this, e)) return;

            if (e.KeyModifiers == App.ControlKey) {
                if (e.Key == Key.N) New(sender, e);
                else if (e.Key == Key.O) Open(sender, e);
            }
        }

        void Window_KeyDown(object sender, KeyEventArgs e) {
            List<Window> windows = App.Windows.ToList();
            HandleKey(sender, e);
            
            if (windows.SequenceEqual(App.Windows) && FocusManager.Instance.Current?.GetType() != typeof(TextBox))
                this.Focus();
        }

        void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (CrashPanel.IsHitTestVisible || !e.Data.Contains(DataFormats.FileNames)) e.DragEffects = DragDropEffects.None; 
        }

        void Drop(object sender, DragEventArgs e) {
            e.Handled = true;

            if (e.Data.Contains(DataFormats.FileNames)) {
                string path = e.Data.GetFileNames().FirstOrDefault();

                if (path != null) ReadFile(path);

                return;
            }
        }

        void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag(e);
        
        void Minimize() => WindowState = WindowState.Minimized;
    }
}
