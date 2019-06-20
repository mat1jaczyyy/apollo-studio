using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

using Apollo.Binary;
using Apollo.Components;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class ProjectWindow: Window, ISelectParentViewer {
        public int? IExpanded {
            get => null;
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void UpdateTitle() => Title = TitleText.Text = (Program.Project.FilePath == "")? "New Project" : Program.Project.FileName;

        private void UpdatePage() => Page.RawValue = Program.Project.Page;
        private void HandlePage() => Dispatcher.UIThread.InvokeAsync((Action)UpdatePage);

        private void UpdateTopmost(bool value) => Topmost = value;

        TextBlock TitleText;

        ContextMenu TrackContextMenu;
        Controls Contents;
        TrackAdd TrackAdd;

        TextBox BPM;
        HorizontalDial Page;
        
        private void SetAlwaysShowing() {
            TrackAdd.AlwaysShowing = (Contents.Count == 1);

            for (int i = 1; i < Contents.Count; i++)
                ((TrackInfo)Contents[i]).TrackAdd.AlwaysShowing = false;

            if (Contents.Count > 1) ((TrackInfo)Contents.Last()).TrackAdd.AlwaysShowing = true;
        }

        public void Contents_Insert(int index, Track track) {
            TrackInfo viewer = new TrackInfo(track);
            viewer.TrackAdded += Track_Insert;
            track.Info = viewer;

            Contents.Insert(index + 1, viewer);
            SetAlwaysShowing();
        }

        public void Contents_Remove(int index) {
            Contents.RemoveAt(index + 1);
            SetAlwaysShowing();
        }

        public SelectionManager Selection = new SelectionManager();
        
        public ProjectWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            TitleText = this.Get<TextBlock>("Title");

            TrackContextMenu = (ContextMenu)this.Resources["TrackContextMenu"];
            TrackContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(TrackContextMenu_Click));
            
            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);

            Contents = this.Get<StackPanel>("Contents").Children;
            TrackAdd = this.Get<TrackAdd>("TrackAdd");

            for (int i = 0; i < Program.Project.Count; i++)
                Contents_Insert(i, Program.Project[i]);
            
            BPM = this.Get<TextBox>("BPM");
            BPM.Text = Program.Project.BPM.ToString(CultureInfo.InvariantCulture);

            BPM.GetObservable(TextBox.TextProperty).Subscribe(BPM_Changed);

            Page = this.Get<HorizontalDial>("Page");
            Page.RawValue = Program.Project.Page;
        }
        
        public void Expand(int? index) {}
        
        private void Loaded(object sender, EventArgs e) {
            Position = new PixelPoint(Position.X, Math.Max(0, Position.Y));
            
            Program.Project.PathChanged += UpdateTitle;
            UpdateTitle();

            Program.Project.PageChanged += HandlePage;
            UpdatePage();
        }

        private void Unloaded(object sender, CancelEventArgs e) {
            Program.Project.Window = null;

            Program.Project.PathChanged -= UpdateTitle;
            Preferences.AlwaysOnTopChanged -= UpdateTopmost;

            Program.WindowClose(this);
        }

        private void Track_Insert(int index) => Track_Insert(index, new Track());
        private void Track_InsertStart() => Track_Insert(0);

        private void Track_Insert(int index, Track track) {
            Track r = track.Clone();

            Program.Project.Undo.Add($"Track {index + 1} Inserted", () => {
                Program.Project.Remove(index);
            }, () => {
                Program.Project.Insert(index, r.Clone());
            });
            
            Program.Project.Insert(index, track);
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e) {
            if (await Program.Project.HandleKey(this, e) || Program.Project.Undo.HandleKey(e) || Selection.HandleKey(e))
                return;

            if (e.Key == Key.Up) Selection.Move(false, e.Modifiers == InputModifiers.Shift);
            else if (e.Key == Key.Down) Selection.Move(true, e.Modifiers == InputModifiers.Shift);
            else if (e.Key == Key.Enter)
                foreach (ISelect i in Selection.Selection)
                    TrackWindow.Create((Track)i, this);
        }

        private void Page_Changed(double value, double? old) => Program.Project.Page = (int)value;

        private Action BPM_Update;
        private bool BPM_Dirty = false;
        private int BPM_Clean = Program.Project.BPM;
        private bool BPM_Ignore = false;

        private void BPM_Changed(string text) {
            if (text == null) return;
            if (text == "") text = "0";

            BPM_Update = () => { BPM.Text = Program.Project.BPM.ToString(CultureInfo.InvariantCulture); };

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

                    if (value > 999) text = "999";
                    
                    BPM.Text = text;
                };
            }

            Dispatcher.UIThread.InvokeAsync(() => {
                BPM_Update?.Invoke();
                BPM_Update = null;
            });
        }
        
        private void BPM_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Return) 
                this.Focus();

            e.Key = Key.None;
        }

        private void BPM_KeyUp(object sender, KeyEventArgs e) => e.Key = Key.None;

        private void BPM_Unfocus(object sender, RoutedEventArgs e) {
            if (BPM_Clean != Program.Project.BPM) {
                int u = BPM_Clean;
                int r = BPM_Clean = Program.Project.BPM;

                Program.Project.Undo.Add($"BPM Changed to {r}", () => {
                    Program.Project.BPM = u;
                }, () => {
                    Program.Project.BPM = r;
                });
            }

            BPM_Dirty = false;
        }

        public void SetBPM(string bpm) {
            if (BPM_Ignore) return;

            BPM.Text = bpm;
            BPM_Dirty = false;

            this.Focus();
        }

        private void Clear() {
            foreach (Launchpad lp in MIDI.Devices)
                lp.Clear();
        }

        private void Window_Focus(object sender, PointerPressedEventArgs e) => this.Focus();

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
        
        private void Minimize() => WindowState = WindowState.Minimized;

        private async void CheckClose(bool force) {
            if (force || Program.Project.Tracks.FirstOrDefault(i => i.Window != null) == null) {
                string result = Program.Project.Undo.Saved? "No" : await MessageWindow.Create(
                    "You have unsaved changes. Do you want to save before closing?\n",
                    new string[] {"Yes", "No", "Cancel"}, this
                );

                if (result == "No" || (result == "Yes" && await Program.Project.Save(this))) {
                    if (force)
                        foreach (Track track in Program.Project.Tracks) track.Window?.Close();

                    Close();
                }
                
            } else Close();
        }

        private void ResizeNorth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.North);

        private void ResizeSouth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.South);

        public static void Create(Window owner) {
            if (Program.Project.Window == null) {
                Program.Project.Window = new ProjectWindow() {Owner = owner};
                Program.Project.Window.Show();
                Program.Project.Window.Owner = null;
            } else {
                Program.Project.Window.WindowState = WindowState.Normal;
                Program.Project.Window.Activate();
            }
        }

        private void Track_Action(string action) => Track_Action(action, false);
        private void Track_Action(string action, bool right) => Program.Project.Window?.Selection.Action(action, Program.Project, (right? Program.Project.Count : 0) - 1);

        private void TrackContextMenu_Click(object sender, EventArgs e) {
            this.Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Track_Action((string)((MenuItem)item).Header, true);
        }

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Right)
                TrackContextMenu.Open((Control)sender);

            e.Handled = true;
        }

        private void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.Contains("track")) e.DragEffects = DragDropEffects.None; 
        }

        private void Drop(object sender, DragEventArgs e) {
            e.Handled = true;

            if (!e.Data.Contains("track")) return;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZoneAfter" && source.Name != "TrackAdd") {
                source = source.Parent;
                
                if (source == this) {
                    e.Handled = false;
                    return;
                }
            }

            List<Track> moving = ((List<ISelect>)e.Data.Get("track")).Select(i => (Track)i).ToList();

            int before = moving[0].IParentIndex.Value - 1;
            int after = (source.Name == "DropZoneAfter")? Program.Project.Count - 1 : -1;

            bool copy = e.Modifiers.HasFlag(InputModifiers.Control);

            bool result = Track.Move(moving, Program.Project, after, copy);

            if (result) {
                int before_pos = before;
                int after_pos = moving[0].IParentIndex.Value - 1;
                int count = moving.Count;

                if (after < before)
                    before_pos += count;
                
                Program.Project.Undo.Add($"Track {(copy? "Copied" : "Moved")}", copy
                    ? new Action(() => {
                        for (int i = after + count; i > after; i--)
                            Program.Project.Remove(i);

                    }) : new Action(() => {
                        List<Track> umoving = (from i in Enumerable.Range(after_pos + 1, count) select Program.Project[i]).ToList();

                        Track.Move(umoving, Program.Project, before_pos);

                }), () => {
                    List<Track> rmoving = (from i in Enumerable.Range(before + 1, count) select Program.Project[i]).ToList();

                    Track.Move(rmoving, Program.Project, after, copy);
                });
            
            } else e.DragEffects = DragDropEffects.None;
        }

        private void Copyable_Insert(Copyable paste, int right, bool imported) {
            List<Track> pasted;
            try {
                pasted = paste.Contents.Cast<Track>().ToList();
            } catch (InvalidCastException) {
                return;
            }

            Program.Project.Undo.Add($"Track {(imported? "Imported" : "Pasted")}", () => {
                for (int i = paste.Contents.Count - 1; i >= 0; i--)
                    Program.Project.Remove(right + i + 1);

            }, () => {
                for (int i = 0; i < paste.Contents.Count; i++)
                    Program.Project.Insert(right + i + 1, pasted[i].Clone());
            });
            
            for (int i = 0; i < paste.Contents.Count; i++)
                Program.Project.Insert(right + i + 1, pasted[i].Clone());
        }

        public async void Copy(int left, int right, bool cut = false) {
            Copyable copy = new Copyable();
            
            for (int i = left; i <= right; i++)
                copy.Contents.Add(Program.Project[i]);

            string b64 = Convert.ToBase64String(Encoder.Encode(copy).ToArray());

            if (cut) Delete(left, right);
            
            await Application.Current.Clipboard.SetTextAsync(b64);
        }

        public async void Paste(int right) {
            string b64 = await Application.Current.Clipboard.GetTextAsync();

            if (b64 == null) return;
            
            Copyable paste;
            try {
                paste = await Decoder.Decode(new MemoryStream(Convert.FromBase64String(b64)), typeof(Copyable));
            } catch (Exception) {
                return;
            }

            Copyable_Insert(paste, right, false);
        }

        public void Duplicate(int left, int right) {
            Program.Project.Undo.Add($"Track Duplicated", () => {
                for (int i = right - left; i >= 0; i--)
                    Program.Project.Remove(right + i + 1);

            }, () => {
                for (int i = 0; i <= right - left; i++)
                    Program.Project.Insert(right + i + 1, Program.Project[left + i].Clone());
            });

            for (int i = 0; i <= right - left; i++)
                Program.Project.Insert(right + i + 1, Program.Project[left + i].Clone());
        }

        public void Delete(int left, int right) {
            List<Track> ut = (from i in Enumerable.Range(left, right - left + 1) select Program.Project[i].Clone()).ToList();
            List<Launchpad> ul = (from i in Enumerable.Range(left, right - left + 1) select Program.Project[i].Launchpad).ToList();

            Program.Project.Undo.Add($"Track Removed", () => {
                for (int i = left; i <= right; i++) {
                    Track restored = ut[i - left].Clone();
                    restored.Launchpad = ul[i - left];
                    Program.Project.Insert(i, restored);
                }
                
            }, () => {
                for (int i = right; i >= left; i--)
                    Program.Project.Remove(i);
            });

            for (int i = right; i >= left; i--)
                Program.Project.Remove(i);
        }

        public void Group(int left, int right) {}
        public void Ungroup(int index) {}
        
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
        
        public async void Import(int right) {
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

            if (result.Length > 0) {
                Copyable loaded;

                using (FileStream file = File.Open(result[0], FileMode.Open, FileAccess.Read))
                    try {
                        loaded = await Decoder.Decode(file, typeof(Copyable));

                    } catch {
                        await MessageWindow.Create(
                            $"An error occurred while reading the file.\n\n" +
                            "You may not have sufficient privileges to read from the destination folder, or\n" +
                            "the file you're attempting to read is invalid.",
                            null, this
                        );

                        return;
                    }
                
                Copyable_Insert(loaded, right, true);
            }
        }
    }
}