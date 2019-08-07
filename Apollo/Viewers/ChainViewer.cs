using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

using Apollo.Binary;
using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Interfaces;
using Apollo.Windows;

namespace Apollo.Viewers {
    public class ChainViewer: UserControl, ISelectParentViewer {
        public int? IExpanded {
            get => null;
        }

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            DropZoneBefore = this.Get<Grid>("DropZoneBefore");
            DropZoneAfter = this.Get<Grid>("DropZoneAfter");

            Contents = this.Get<StackPanel>("Contents").Children;
            DeviceAdd = this.Get<DeviceAdd>("DeviceAdd");
        }
        
        Chain _chain;

        Grid DropZoneBefore, DropZoneAfter;
        ContextMenu DeviceContextMenuBefore, DeviceContextMenuAfter;

        Controls Contents;
        DeviceAdd DeviceAdd;

        void SetAlwaysShowing() {
            bool RootChain = _chain.Parent.GetType() == typeof(Track);

            DeviceAdd.AlwaysShowing = Contents.Count == 1;

            for (int i = 1; i < Contents.Count; i++)
                ((DeviceViewer)Contents[i]).DeviceAdd.AlwaysShowing = false;

            if (Contents.Count > 1 && RootChain) ((DeviceViewer)Contents.Last()).DeviceAdd.AlwaysShowing = true;
        }

        public void Contents_Insert(int index, Device device) {
            DeviceViewer viewer = device.Collapsed? (DeviceViewer)new CollapsedDeviceViewer(device) : new DeviceViewer(device);
            viewer.Added += Device_Insert;
            viewer.DeviceCollapsed += Device_Collapsed;

            Contents.Insert(index + 1, viewer);
            SetAlwaysShowing();
        }

        public void Contents_Remove(int index) {
            Contents.RemoveAt(index + 1);
            SetAlwaysShowing();
        }

        public ChainViewer() => new InvalidOperationException();

        public ChainViewer(Chain chain, bool backgroundBorder = false) {
            InitializeComponent();

            _chain = chain;
            _chain.Viewer = this;
            
            DeviceContextMenuBefore = (ContextMenu)this.Resources["DeviceContextMenuBefore"];
            DeviceContextMenuBefore.AddHandler(MenuItem.ClickEvent, DeviceContextMenu_Click);
            
            DeviceContextMenuAfter = (ContextMenu)this.Resources["DeviceContextMenuAfter"];
            DeviceContextMenuAfter.AddHandler(MenuItem.ClickEvent, DeviceContextMenu_Click);

            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);

            for (int i = 0; i < _chain.Count; i++)
                Contents_Insert(i, _chain[i]);
            
            if (backgroundBorder) {
                this.Get<Grid>("Root").Children.Insert(0, new DeviceBackground());
                Background = (IBrush)Application.Current.Styles.FindResource("ThemeControlDarkenBrush");
            }
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            _chain.Viewer = null;
            _chain = null;

            DeviceContextMenuBefore.RemoveHandler(MenuItem.ClickEvent, DeviceContextMenu_Click);
            DeviceContextMenuAfter.RemoveHandler(MenuItem.ClickEvent, DeviceContextMenu_Click);
            DeviceContextMenuBefore = DeviceContextMenuAfter = null;
            
            this.RemoveHandler(DragDrop.DropEvent, Drop);
            this.RemoveHandler(DragDrop.DragOverEvent, DragOver);
        }

        public void Expand(int? index) {}

        void Device_Insert(int index, Type device) => Device_Insert(index, Device.Create(device, _chain));
        void Device_InsertStart(Type device) => Device_Insert(0, device);

        void Device_Insert(int index, Device device) {
            Device r = device.Clone();
            List<int> path = Track.GetPath(_chain);

            Program.Project.Undo.Add($"Device ({r.GetType().ToString().Split(".").Last()}) Inserted", () => {
                ((Chain)Track.TraversePath(path)).Remove(index);
            }, () => {
                ((Chain)Track.TraversePath(path)).Insert(index, r.Clone());
            }, () => {
                r.Dispose();
            });
            
            _chain.Insert(index, device);
        }

        void Device_Collapsed(int index) {
            Contents_Remove(index);
            Contents_Insert(index, _chain[index]);
            
            Track.Get(_chain[index]).Window?.Selection.Select(_chain[index]);
        }

        void Device_Action(string action) => Device_Action(action, false);
        void Device_Action(string action, bool right) => Track.Get(_chain)?.Window?.Selection.Action(action, _chain, (right? _chain.Count : 0) - 1);

        void DeviceContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Device_Action((string)((MenuItem)item).Header, sender == DeviceContextMenuAfter);
        }

        void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Right) 
                if (sender == DropZoneBefore) DeviceContextMenuBefore.Open((Control)sender);
                else if (sender == DropZoneAfter) DeviceContextMenuAfter.Open((Control)sender);

            e.Handled = true;
        }

        void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.Contains("device") && !e.Data.Contains(DataFormats.FileNames)) e.DragEffects = DragDropEffects.None; 
        }

        void Drop(object sender, DragEventArgs e) {
            e.Handled = true;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZoneBefore" && source.Name != "DropZoneAfter" && source.Name != "DeviceAdd") {
                source = source.Parent;
                
                if (source == this) {
                    e.Handled = false;
                    return;
                }
            }

            int after = (source.Name == "DropZoneAfter")? _chain.Count - 1 : -1;

            if (e.Data.Contains(DataFormats.FileNames)) {
                string path = e.Data.GetFileNames().FirstOrDefault();

                if (path != null) Import(after, path);

                return;
            }

            if (!e.Data.Contains("device")) return;

            List<Device> moving = ((List<ISelect>)e.Data.Get("device")).Select(i => (Device)i).ToList();
            Chain source_parent = moving[0].Parent;
            int before = moving[0].IParentIndex.Value - 1;

            bool copy = e.Modifiers.HasFlag(App.ControlKey);

            bool result = Device.Move(moving, _chain, after, copy);
            
            if (result) {
                int before_pos = before;
                int after_pos = moving[0].IParentIndex.Value - 1;
                int count = moving.Count;

                if (source_parent == _chain && after < before)
                    before_pos += count;
                
                List<int> sourcepath = Track.GetPath(source_parent);
                List<int> targetpath = Track.GetPath(_chain);
                
                Program.Project.Undo.Add($"Device {(copy? "Copied" : "Moved")}", copy
                    ? new Action(() => {
                        Chain targetchain = ((Chain)Track.TraversePath(targetpath));

                        for (int i = after + count; i > after; i--)
                            targetchain.Remove(i);

                    }) : new Action(() => {
                        Chain sourcechain = ((Chain)Track.TraversePath(sourcepath));
                        Chain targetchain = ((Chain)Track.TraversePath(targetpath));

                        List<Device> umoving = (from i in Enumerable.Range(after_pos + 1, count) select targetchain[i]).ToList();

                        Device.Move(umoving, sourcechain, before_pos);

                }), () => {
                    Chain sourcechain = ((Chain)Track.TraversePath(sourcepath));
                    Chain targetchain = ((Chain)Track.TraversePath(targetpath));

                    List<Device> rmoving = (from i in Enumerable.Range(before + 1, count) select sourcechain[i]).ToList();

                    Device.Move(rmoving, targetchain, after, copy);
                });
            
            } else e.DragEffects = DragDropEffects.None;
        }

        void Copyable_Insert(Copyable paste, int right, bool imported) {
            List<Device> pasted;
            try {
                pasted = paste.Contents.Cast<Device>().ToList();
            } catch (InvalidCastException) {
                return;
            }
            
            List<int> path = Track.GetPath(_chain);

            Program.Project.Undo.Add($"Device {(imported? "Imported" : "Pasted")}", () => {
                Chain chain = ((Chain)Track.TraversePath(path));

                for (int i = paste.Contents.Count - 1; i  >= 0; i--)
                    chain.Remove(right + i + 1);

            }, () => {
                Chain chain = ((Chain)Track.TraversePath(path));

                for (int i = 0; i < paste.Contents.Count; i++)
                    chain.Insert(right + i + 1, pasted[i].Clone());

            }, () => {
                foreach (Device device in pasted) device.Dispose();
                pasted = null;
            });

            for (int i = 0; i < paste.Contents.Count; i++)
                _chain.Insert(right + i + 1, pasted[i].Clone());
        }

        public async void Copy(int left, int right, bool cut = false) {
            Copyable copy = new Copyable();
            
            for (int i = left; i <= right; i++)
                copy.Contents.Add(_chain[i]);

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
            List<int> path = Track.GetPath(_chain);

            Program.Project.Undo.Add($"Device Duplicated", () => {
                Chain chain = ((Chain)Track.TraversePath(path));

                for (int i = right - left; i >= 0; i--)
                    chain.Remove(right + i + 1);

            }, () => {
                Chain chain = ((Chain)Track.TraversePath(path));

                for (int i = 0; i <= right - left; i++)
                    chain.Insert(right + i + 1, chain[left + i].Clone());
            });

            for (int i = 0; i <= right - left; i++)
                _chain.Insert(right + i + 1, _chain[left + i].Clone());
        }

        public void Delete(int left, int right) {
            List<Device> u = (from i in Enumerable.Range(left, right - left + 1) select _chain[i].Clone()).ToList();

            List<int> path = Track.GetPath(_chain);

            Program.Project.Undo.Add($"Device Removed", () => {
                Chain chain = ((Chain)Track.TraversePath(path));

                for (int i = left; i <= right; i++)
                    chain.Insert(i, u[i - left].Clone());

            }, () => {
                Chain chain = ((Chain)Track.TraversePath(path));

                for (int i = right; i >= left; i--)
                    chain.Remove(i);
            
            }, () => {
               foreach (Device device in u) device.Dispose();
               u = null;
            });

            for (int i = right; i >= left; i--)
                _chain.Remove(i);
        }

        public void Group(int left, int right) {
            Chain init = new Chain();

            for (int i = left; i <= right; i++)
                init.Add(_chain.Devices[i].Clone());

            List<int> path = Track.GetPath(_chain);

            Program.Project.Undo.Add($"Device Grouped", () => {
                Chain chain = ((Chain)Track.TraversePath(path));

                chain.Remove(left);

                for (int i = left; i <= right; i++)
                    chain.Insert(i, init[i - left].Clone());
                
                Track track = Track.Get(chain);
                track?.Window?.Selection.Select(chain[left]);
                track?.Window?.Selection.Select(chain[right], true);

            }, () => {
                Chain chain = ((Chain)Track.TraversePath(path));
                
                for (int i = right; i >= left; i--)
                    chain.Remove(i);
                
                chain.Insert(left, new Group(new List<Chain>() {init.Clone()}) {Expanded = 0});
            
            }, () => {
                init.Dispose();
            });
            
            for (int i = right; i >= left; i--)
                _chain.Remove(i);

            _chain.Insert(left, new Group(new List<Chain>() {init.Clone()}) {Expanded = 0});
        }

        public void Ungroup(int index) {
            if (_chain.Devices[index].GetType() != typeof(Group)) return;

            List<Device> init = (from i in ((Group)_chain.Devices[index])[0].Devices select i.Clone()).ToList();

            List<int> path = Track.GetPath(_chain);

            Program.Project.Undo.Add($"Device Ungrouped", () => {
                Chain chain = ((Chain)Track.TraversePath(path));

                for (int i = index + init.Count - 1; i >= index; i--)
                    chain.Remove(i);
                
                Chain group = new Chain();

                for (int i = 0; i < init.Count; i++)
                    group.Add(init[i].Clone());
                
                chain.Insert(index, new Group(new List<Chain>() {group}) {Expanded = 0});

            }, () => {
                Chain chain = ((Chain)Track.TraversePath(path));
                
                chain.Remove(index);
            
                for (int i = 0; i < init.Count; i++)
                    chain.Insert(index + i, init[i].Clone());
                    
                if (init.Count > 0) {
                    Track track = Track.Get(chain);
                    track?.Window?.Selection.Select(chain[index]);
                    track?.Window?.Selection.Select(chain[index + init.Count - 1], true);
                }
                
            }, () => {
                foreach (Device device in init) device.Dispose();
                init = null;
            });

            _chain.Remove(index);
            
            for (int i = 0; i < init.Count; i++)
                _chain.Insert(index + i, init[i].Clone());

            if (init.Count > 0) {
                Track _track = Track.Get(_chain);
                _track?.Window?.Selection.Select(_chain[index]);
                _track?.Window?.Selection.Select(_chain[index + init.Count - 1], true);
            }
        }
        
        public void Mute(int left, int right) {
            List<bool> u = (from i in Enumerable.Range(left, right - left + 1) select _chain[i].Enabled).ToList();
            bool r = !_chain[left].Enabled;

            List<int> path = Track.GetPath(_chain);

            Program.Project.Undo.Add($"Device Muted", () => {
                Chain chain = ((Chain)Track.TraversePath(path));

                for (int i = left; i <= right; i++)
                    chain[i].Enabled = u[i - left];

            }, () => {
                Chain chain = ((Chain)Track.TraversePath(path));

                for (int i = left; i <= right; i++)
                    chain[i].Enabled = r;
            });

            for (int i = left; i <= right; i++)
                _chain[i].Enabled = r;
        }

        public void Rename(int left, int right) {}

        public async void Export(int left, int right) {
            Window sender = Track.Get(_chain).Window;

            SaveFileDialog sfd = new SaveFileDialog() {
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                        Extensions = new List<string>() {
                            "apdev"
                        },
                        Name = "Apollo Device Preset"
                    }
                },
                Title = "Export Device Preset"
            };
            
            string result = await sfd.ShowAsync(sender);

            if (result != null) {
                string[] file = result.Split(Path.DirectorySeparatorChar);

                if (Directory.Exists(string.Join("/", file.Take(file.Count() - 1)))) {
                    Copyable copy = new Copyable();
                    
                    for (int i = left; i <= right; i++)
                        copy.Contents.Add(_chain[i]);

                    try {
                        File.WriteAllBytes(result, Encoder.Encode(copy).ToArray());

                    } catch (UnauthorizedAccessException) {
                        await MessageWindow.Create(
                            $"An error occurred while writing the file.\n\n" +
                            "You may not have sufficient privileges to write to the destination folder, or\n" +
                            "the current file already exists but cannot be overwritten.",
                            null, sender
                        );
                    }
                }
            }
        }
        
        public async void Import(int right, string path = null) {
            Window sender = Track.Get(_chain).Window;

            if (path == null) {
                OpenFileDialog ofd = new OpenFileDialog() {
                    AllowMultiple = false,
                    Filters = new List<FileDialogFilter>() {
                        new FileDialogFilter() {
                            Extensions = new List<string>() {
                                "apdev"
                            },
                            Name = "Apollo Device Preset"
                        }
                    },
                    Title = "Import Device Preset"
                };

                string[] result = await ofd.ShowAsync(sender);

                if (result.Length > 0) path = result[0];
                else return;
            }
        
            Copyable loaded;

            try {
                using (FileStream file = File.Open(path, FileMode.Open, FileAccess.Read))
                    loaded = await Decoder.Decode(file, typeof(Copyable));

            } catch {
                await MessageWindow.Create(
                    $"An error occurred while reading the file.\n\n" +
                    "You may not have sufficient privileges to read from the destination folder, or\n" +
                    "the file you're attempting to read is invalid.",
                    null, sender
                );

                return;
            }
            
            Copyable_Insert(loaded, right, true);
        }
    }
}
