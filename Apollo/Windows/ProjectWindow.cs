using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

using Humanizer;
using Humanizer.Localisation;

using Apollo.Binary;
using Apollo.Components;
using Apollo.Core;
using Apollo.DragDrop;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Selection;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class ProjectWindow: Window, ISelectParentViewer, IDroppable {
        public int? IExpanded {
            get => null;
        }

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            TitleText = this.Get<TextBlock>("Title");
            TitleCenter = this.Get<TextBlock>("TitleCenter");

            CenteringLeft = this.Get<StackPanel>("CenteringLeft");
            CenteringRight = this.Get<StackPanel>("CenteringRight");

            Contents = this.Get<StackPanel>("Contents").Children;
            TrackAdd = this.Get<TrackAdd>("TrackAdd");
            
            MacroDials = this.Get<StackPanel>("MacroDials");

            BPM = this.Get<TextBox>("BPM");
            
            Author = this.Get<TextBox>("Author");

            BottomPane = this.Get<StackPanel>("BottomPane");
            CollapseButton = this.Get<CollapseButton>("CollapseButton");

            TimeSpent = this.Get<TextBlock>("TimeSpent");
            Started = this.Get<TextBlock>("Started");
        }

        HashSet<IDisposable> observables = new HashSet<IDisposable>();

        TextBlock TitleText, TitleCenter, TimeSpent, Started;
        StackPanel CenteringLeft, CenteringRight, MacroDials, BottomPane;
        CollapseButton CollapseButton;

        Controls Contents;
        TrackAdd TrackAdd;

        TextBox BPM, Author;

        DispatcherTimer Timer;
        bool SafeClose = false;

        void UpdateTitle() => Title = TitleText.Text = TitleCenter.Text = (Program.Project.FilePath == "")? "New Project" : Program.Project.FileName;

        void UpdateMacro() {
            for (int i = 0; i < 4; i++)
                ((Dial)MacroDials.Children[i]).RawValue = Program.Project.GetMacro(i + 1);
        }
        
        void HandleMacro() => Dispatcher.UIThread.InvokeAsync((Action)UpdateMacro);

        void UpdateTopmost(bool value) => Topmost = value;

        void UpdateTime(object sender, EventArgs e) => TimeSpent.Text = $"Total time spent: {Program.Project.Time.Seconds().Humanize(minUnit: TimeUnit.Second, maxUnit: TimeUnit.Hour)}";
        
        void SetAlwaysShowing() {
            TrackAdd.AlwaysShowing = (Contents.Count == 1);

            for (int i = 1; i < Contents.Count; i++)
                ((TrackInfo)Contents[i]).TrackAdd.AlwaysShowing = false;

            if (Contents.Count > 1) ((TrackInfo)Contents.Last()).TrackAdd.AlwaysShowing = true;
        }

        public void Contents_Insert(int index, Track track) {
            TrackInfo viewer = new TrackInfo(track);
            viewer.Added += Track_Insert;
            track.Info = viewer;

            Contents.Insert(index + 1, viewer);
            SetAlwaysShowing();
        }

        public void Contents_Remove(int index) {
            Contents.RemoveAt(index + 1);
            SetAlwaysShowing();
        }

        public SelectionManager Selection;
        
        public ProjectWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;
            
            DragDrop = new DragDropManager(this);

            for (int i = 0; i < Program.Project.Count; i++)
                Contents_Insert(i, Program.Project[i]);

            Selection = new SelectionManager(() => Program.Project.Tracks.FirstOrDefault());
            
            BPM.Text = Program.Project.BPM.ToString();
            observables.Add(BPM.GetObservable(TextBox.TextProperty).Subscribe(BPM_Changed));

            for(int i = 0; i < 4; i++){
                ((Dial)MacroDials.Children[i]).RawValue = Program.Project.GetMacro(i + 1);
            }
            
            Author.Text = Program.Project.Author.ToString();
            observables.Add(Author.GetObservable(TextBox.TextProperty).Subscribe(Author_Changed));

            UpdateTime(null, EventArgs.Empty);
            Timer = new DispatcherTimer() {
                Interval = new TimeSpan(0, 0, 1)
            };
            Timer.Tick += UpdateTime;
            Timer.Start();

            Started.Text = $"Started {Program.Project.Started.LocalDateTime.ToString("MM/dd/yyyy HH:mm")}";

            observables.Add(this.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated));
            observables.Add(TitleText.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated));
            observables.Add(TitleCenter.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated));
            observables.Add(CenteringLeft.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated));
            observables.Add(CenteringRight.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated));
        }
        
        void Loaded(object sender, EventArgs e) {
            Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));
            
            Program.Project.PathChanged += UpdateTitle;
            UpdateTitle();

            Program.Project.MacroChanged += HandleMacro;
            UpdateMacro();
        }

        void Unloaded(object sender, CancelEventArgs e) {
            if (!SafeClose) {
                e.Cancel = true;

                CloseForce(false);
                return;
            }

            Program.Project.Window = null;

            Timer.Stop();
            Timer.Tick -= UpdateTime;

            Program.Project.PathChanged -= UpdateTitle;
            Program.Project.MacroChanged -= HandleMacro;
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;

            Selection.Dispose();
            
            DragDrop.Dispose();
            DragDrop = null;

            foreach (IDisposable observable in observables)
                observable.Dispose();

            this.Content = null;

            App.WindowClosed(this);
        }
        
        public void Bounds_Updated(Rect bounds) {
            if (Bounds.IsEmpty || TitleText.Bounds.IsEmpty || TitleCenter.Bounds.IsEmpty || CenteringLeft.Bounds.IsEmpty || CenteringRight.Bounds.IsEmpty) return;

            int result = Convert.ToInt32((Bounds.Width - TitleText.Bounds.Width) / 2 <= Math.Max(CenteringLeft.Bounds.Width, CenteringRight.Bounds.Width) + 10);

            TitleText.Opacity = result;
            TitleCenter.Opacity = 1 - result;
        }
        
        public void Expand(int? index) {}

        void Track_Insert(int index) => Track_Insert(index, new Track());
        void Track_InsertStart() => Track_Insert(0);

        void Track_Insert(int index, Track track) {
            Program.Project.Undo.AddAndExecute(new Project.TrackInsertedUndoEntry(
                index,
                track
            ));
        }

        async void HandleKey(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            if (App.WindowKey(this, e) || await Program.Project.HandleKey(this, e) || Program.Project.Undo.HandleKey(e) || Selection.HandleKey(e))
                return;

            if (e.KeyModifiers != KeyModifiers.None && e.KeyModifiers != KeyModifiers.Shift) return;

            if (e.Key == Key.Up) Selection.Move(false, e.KeyModifiers == KeyModifiers.Shift);
            else if (e.Key == Key.Down) Selection.Move(true, e.KeyModifiers == KeyModifiers.Shift);

            else if (e.Key == Key.Enter)
                foreach (ISelect i in Selection.Selection)
                    TrackWindow.Create((Track)i, this);
        }

        void Window_KeyDown(object sender, KeyEventArgs e) {
            List<Window> windows = App.Windows.ToList();
            HandleKey(sender, e);
            
            if (windows.SequenceEqual(App.Windows) && FocusManager.Instance.Current?.GetType() != typeof(TextBox))
                this.Focus();
        }

        void Window_LostFocus(object sender, RoutedEventArgs e) {
            if (FocusManager.Instance.Current?.GetType() == typeof(ComboBox))
                this.Focus();
        }

        void Macro_Changed(Dial sender, double value, double? old){
          Program.Project.SetMacro(MacroDials.Children.IndexOf(sender) + 1, (int)value);
        }

        Action BPM_Update;
        bool BPM_Dirty = false;
        int BPM_Clean = Program.Project.BPM;
        bool BPM_Ignore = false;

        void BPM_Changed(string text) {
            if (text == null) return;
            if (text == "") text = "0";

            BPM_Update = () => { BPM.Text = Program.Project.BPM.ToString(); };

            if (int.TryParse(text, out int value)) {
                if (20 <= value && value <= 999) {
                    if (!BPM_Dirty && value != Program.Project.BPM) {
                        BPM_Clean = Program.Project.BPM;
                        BPM_Dirty = true;
                    }

                    BPM_Ignore = true;
                    Program.Project.BPM = value;
                    BPM_Ignore = false;
                    
                    BPM_Update = () => { BPM.Foreground = (IBrush)Application.Current.Styles.FindResource("ThemeForegroundBrush"); };
                } else {
                    BPM_Update = () => { BPM.Foreground = (IBrush)Application.Current.Styles.FindResource("ErrorBrush"); };
                }

                BPM_Update += () => { 
                    if (value <= 0) text = "0";
                    else text = text.TrimStart('0');

                    if (value > 999) {
                        text = "999";
                        BPM.Foreground = (IBrush)Application.Current.Styles.FindResource("ThemeForegroundBrush");
                    }
                    
                    BPM.Text = text;
                };
            }

            Dispatcher.UIThread.InvokeAsync(() => {
                BPM_Update?.Invoke();
                BPM_Update = null;
            });
        }

        void Text_MouseUp(object sender, PointerReleasedEventArgs e) => ((TextBox)sender).Focus();
        
        void Text_KeyDown(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            if (e.Key == Key.Return) 
                this.Focus();

            e.Key = Key.None;
        }

        void Text_KeyUp(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            e.Key = Key.None;
        }

        void BPM_Unfocus(object sender, RoutedEventArgs e) {
            if (BPM_Clean != Program.Project.BPM)
                Program.Project.Undo.AddAndExecute(new Project.BPMChangedUndoEntry(
                    BPM_Clean,
                    BPM_Clean = Program.Project.BPM
                ));
                
            BPM_Dirty = false;
        }

        public void SetBPM(string bpm) {
            if (BPM_Ignore) return;

            Dispatcher.UIThread.InvokeAsync(() => {
                BPM.Text = bpm;
                BPM_Dirty = false;

                this.Focus();
            });
        }

        bool Author_Dirty = false;
        string Author_Clean = Program.Project.Author;
        bool Author_Ignore = false;

        void Author_Changed(string text) {
            if (text == null) return;
            
            if (!Author_Dirty && text != Program.Project.Author) {
                Author_Clean = Program.Project.Author;
                Author_Dirty = true;
            }

            Author_Ignore = true;
            Program.Project.Author = text;
            Author_Ignore = false;
        }

        void Author_Unfocus(object sender, RoutedEventArgs e) {
            if (Author_Clean != Program.Project.Author)
                Program.Project.Undo.AddAndExecute(new Project.AuthorChangedUndoEntry(
                    Author_Clean,
                    Author_Clean = Program.Project.Author
                ));

            Author_Dirty = false;
        }

        public void SetAuthor(string author) {
            if (Author_Ignore) return;

            Author.Text = author;
            Author_Dirty = false;

            this.Focus();
        }

        void BottomCollapse() => BottomPane.Opacity = Convert.ToInt32(CollapseButton.Showing = (BottomPane.MaxHeight = (BottomPane.MaxHeight == 0)? 1000 : 0) != 0);

        void Window_Focus(object sender, PointerPressedEventArgs e) => this.Focus();

        void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag(e);
        
        void Minimize() => WindowState = WindowState.Minimized;

        async Task<bool> CheckClose(bool force = false) {
            if (!force && Program.Project.Tracks.FirstOrDefault(i => i.Window != null) != null) return true;

            string result = Program.Project.Undo.Saved? "No" : await MessageWindow.Create(
                "You have unsaved changes. Do you want to save before closing?\n",
                new string[] {"Yes", "No", "Cancel"}, this
            );

            if (result == "No" || (result == "Yes" && await Program.Project.Save(this))) {
                if (force)
                    foreach (Track track in Program.Project.Tracks) track.Window?.Close();

                return true;
            }

            return false;
        }

        public async void CloseForce(bool force) {
            if (SafeClose = await CheckClose(force)) base.Close();
        }

        void ResizeNorth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.North, e);

        void ResizeSouth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.South, e);

        public static void Create(Window owner) {
            if (Program.Project.Window == null) {
                Program.Project.Window = new ProjectWindow();
                
                if (owner == null || owner.WindowState == WindowState.Minimized) 
                    Program.Project.Window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                else
                    Program.Project.Window.Owner = owner;

                Program.Project.Window.Show();
                Program.Project.Window.Owner = null;

            } else {
                Program.Project.Window.WindowState = WindowState.Normal;
                Program.Project.Window.Activate();
            }

            Program.Project.Window.Topmost = true;
            Program.Project.Window.Topmost = Preferences.AlwaysOnTop;
        }

        void Track_Action(string action) => Track_Action(action, false);
        void Track_Action(string action, bool right) => Program.Project.Window?.Selection.Action(action, Program.Project, (right? Program.Project.Count : 0) - 1);

        void ContextMenu_Action(string action) => Track_Action(action, true);

        void Click(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.RightButtonReleased)
                ((ApolloContextMenu)this.Resources["TrackContextMenu"]).Open((Control)sender);

            e.Handled = true;
        }

        DragDropManager DragDrop;

        public List<string> DropAreas => new List<string>() {"DropZoneAfter", "TrackAdd"};

        public Dictionary<string, DragDropManager.DropHandler> DropHandlers => new Dictionary<string, DragDropManager.DropHandler>() {
            {DataFormats.FileNames, null},
            {"Track", null},
        };

        public ISelect Item => null;
        public ISelectParent ItemParent => Program.Project;

        bool Copyable_Insert(Copyable paste, int right, out Action undo, out Action redo, out Action dispose) {
            undo = redo = dispose = null;

            List<Track> pasted;
            try {
                pasted = paste.Contents.Cast<Track>().ToList();
            } catch (InvalidCastException) {
                return false;
            }

            undo = () => {
                for (int i = paste.Contents.Count - 1; i >= 0; i--)
                    Program.Project.Remove(right + i + 1);
            };
            
            redo = () => {
                for (int i = 0; i < paste.Contents.Count; i++)
                    Program.Project.Insert(right + i + 1, pasted[i].Clone());
                
                Program.Project.Window?.Selection.Select(Program.Project[right + 1], true);
            };
            
            dispose = () => {
                foreach (Track track in pasted) track.Dispose();
                pasted = null;
            };
            
            for (int i = 0; i < paste.Contents.Count; i++)
                Program.Project.Insert(right + i + 1, pasted[i].Clone());
            
            Selection.Select(Program.Project[right + 1], true);

            return true;
        }

        void Region_Delete(int left, int right, out Action undo, out Action redo, out Action dispose) {
            List<Track> u = (from i in Enumerable.Range(left, right - left + 1) select Program.Project[i].Clone()).ToList();

            undo = () => {
                for (int i = left; i <= right; i++)
                    Program.Project.Insert(i, u[i - left].Clone());
            };
            
            redo = () => {
                for (int i = right; i >= left; i--)
                    Program.Project.Remove(i);
            };
            
            dispose = () => {
                foreach (Track track in u) track.Dispose();
                u = null;
            };

            for (int i = right; i >= left; i--)
                Program.Project.Remove(i);
        }

        public void Copy(int left, int right, bool cut = false) {
            Copyable copy = new Copyable();
            
            for (int i = left; i <= right; i++)
                copy.Contents.Add(Program.Project[i]);

            copy.StoreToClipboard();

            if (cut) Delete(left, right);
        }

        public async void Paste(int right) {            
            Copyable paste = await Copyable.DecodeClipboard();

            if (paste != null && Copyable_Insert(paste, right, out Action undo, out Action redo, out Action dispose))
                Program.Project.Undo.Add("Track Pasted", undo, redo, dispose);
        }

        public async void Replace(int left, int right) {
            Copyable paste = await Copyable.DecodeClipboard();

            if (paste != null && Copyable_Insert(paste, right, out Action undo, out Action redo, out Action dispose)) {
                Region_Delete(left, right, out Action undo2, out Action redo2, out Action dispose2);

                Program.Project.Undo.Add("Track Replaced",
                    undo2 + undo,
                    redo + redo2 + (() => Program.Project.Window?.Selection.Select(Program.Project[left + paste.Contents.Count - 1], true)),
                    dispose2 + dispose
                );
                
                Selection.Select(Program.Project[left + paste.Contents.Count - 1], true);
            }
        }

        public void Duplicate(int left, int right) {
            Program.Project.Undo.Add($"Track Duplicated", () => {
                for (int i = right - left; i >= 0; i--)
                    Program.Project.Remove(right + i + 1);

            }, () => {
                for (int i = 0; i <= right - left; i++)
                    Program.Project.Insert(right + i + 1, Program.Project[left + i].Clone());
            
                Program.Project.Window?.Selection.Select(Program.Project[right + 1], true);
            });

            for (int i = 0; i <= right - left; i++)
                Program.Project.Insert(right + i + 1, Program.Project[left + i].Clone());
            
            Selection.Select(Program.Project[right + 1], true);
        }

        public void Delete(int left, int right) {
            Region_Delete(left, right, out Action undo, out Action redo, out Action dispose);
            Program.Project.Undo.Add($"Track Removed", undo, redo, dispose);
        }

        public void Group(int left, int right) {}
        public void Ungroup(int index) {}
        public void Choke(int left, int right) {}
        public void Unchoke(int index) {}
        
        public void Mute(int left, int right) {
            List<bool> u = (from i in Enumerable.Range(left, right - left + 1) select Program.Project[i].Enabled).ToList();
            bool r = !Program.Project[left].Enabled;

            Program.Project.Undo.Add($"Track Muted", () => {
                for (int i = left; i <= right; i++)
                    Program.Project[i].Enabled = u[i - left];

            }, () => {
                for (int i = left; i <= right; i++)
                    Program.Project[i].Enabled = r;
            });

            for (int i = left; i <= right; i++)
                Program.Project[i].Enabled = r;
        }

        public void Rename(int left, int right) => ((TrackInfo)Contents[left + 1]).StartInput(left, right);

        public async void Export(int left, int right) {
            SaveFileDialog sfd = new SaveFileDialog() {
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                        Extensions = new List<string>() {
                            "aptrk"
                        },
                        Name = "Apollo Track Preset"
                    }
                },
                Title = "Export Track Preset"
            };
            
            string result = await sfd.ShowAsync(this);

            if (result != null) {
                string[] file = result.Split(Path.DirectorySeparatorChar);

                if (Directory.Exists(string.Join("/", file.Take(file.Count() - 1)))) {
                    Copyable copy = new Copyable();
                    
                    for (int i = left; i <= right; i++)
                        copy.Contents.Add(Program.Project[i]);

                    try {
                        File.WriteAllBytes(result, Encoder.Encode(copy).ToArray());

                    } catch (UnauthorizedAccessException) {
                        await MessageWindow.Create(
                            $"An error occurred while writing the file.\n\n" +
                            "You may not have sufficient privileges to write to the destination folder, or\n" +
                            "the current file already exists but cannot be overwritten.",
                            null, this
                        );
                    }
                }
            }
        }
        
        public async void Import(int right, string path = null) {
            if (path == null) {
                OpenFileDialog ofd = new OpenFileDialog() {
                    AllowMultiple = false,
                    Filters = new List<FileDialogFilter>() {
                        new FileDialogFilter() {
                            Extensions = new List<string>() {
                                "aptrk"
                            },
                            Name = "Apollo Track Preset"
                        }
                    },
                    Title = "Import Track Preset"
                };

                string[] result = await ofd.ShowAsync(this);

                if (result.Length > 0) path = result[0];
                else return;
            }

            Copyable loaded = await Copyable.DecodeFile(path, this);

            if (loaded != null && Copyable_Insert(loaded, right, out Action undo, out Action redo, out Action dispose))
                Program.Project.Undo.Add("Track Imported", undo, redo, dispose);
        }
    }
}