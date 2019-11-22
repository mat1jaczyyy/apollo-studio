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

            Indicator = this.Get<Indicator>("Indicator");
        }
        
        Chain _chain;

        Grid DropZoneBefore, DropZoneAfter;
        ContextMenu DeviceContextMenuBefore, DeviceContextMenuAfter;

        Controls Contents;
        DeviceAdd DeviceAdd;
        public Indicator Indicator { get; private set; }

        void SetAlwaysShowing() {
            bool RootChain = _chain.Parent.GetType() == typeof(Track);

            DeviceAdd.AlwaysShowing = Contents.Count == 1 || RootChain;

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
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.RightButtonReleased)
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

            bool copy = e.Modifiers.HasFlag(App.ControlInput);

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

        bool Copyable_Insert(Copyable paste, int right, out Action undo, out Action redo, out Action dispose) {
            undo = redo = dispose = null;

            List<Device> pasted;
            try {
                pasted = paste.Contents.Cast<Device>().ToList();
            } catch (InvalidCastException) {
                return false;
            }
            
            List<int> path = Track.GetPath(_chain);

            undo = () => {
                Chain chain = ((Chain)Track.TraversePath(path));

                for (int i = paste.Contents.Count - 1; i >= 0; i--)
                    chain.Remove(right + i + 1);
            };
            
            redo = () => {
                Chain chain = ((Chain)Track.TraversePath(path));

                for (int i = 0; i < paste.Contents.Count; i++)
                    chain.Insert(right + i + 1, pasted[i].Clone());
            
                Track.Get(chain).Window?.Selection.Select(chain[right + 1], true);
            };
            
            dispose = () => {
                foreach (Device device in pasted) device.Dispose();
                pasted = null;
            };

            for (int i = 0; i < paste.Contents.Count; i++)
                _chain.Insert(right + i + 1, pasted[i].Clone());
            
            Track.Get(_chain).Window?.Selection.Select(_chain[right + 1], true);
            
            return true;
        }

        void Region_Delete(int left, int right, out Action undo, out Action redo, out Action dispose) {
            List<Device> u = (from i in Enumerable.Range(left, right - left + 1) select _chain[i].Clone()).ToList();

            List<int> path = Track.GetPath(_chain);

            undo = () => {
                Chain chain = ((Chain)Track.TraversePath(path));

                for (int i = left; i <= right; i++)
                    chain.Insert(i, u[i - left].Clone());
            };

            redo = () => {
                Chain chain = ((Chain)Track.TraversePath(path));

                for (int i = right; i >= left; i--)
                    chain.Remove(i);
            };

            dispose = () => {
               foreach (Device device in u) device.Dispose();
               u = null;
            };

            for (int i = right; i >= left; i--)
                _chain.Remove(i);
        }

        public void Copy(int left, int right, bool cut = false) {
            Copyable copy = new Copyable();
            
            for (int i = left; i <= right; i++)
                copy.Contents.Add(_chain[i]);

            copy.StoreToClipboard();

            if (cut) Delete(left, right);
        }

        public async void Paste(int right) {
            Copyable paste = await Copyable.DecodeClipboard();

            if (paste != null && Copyable_Insert(paste, right, out Action undo, out Action redo, out Action dispose))
                Program.Project.Undo.Add("Device Pasted", undo, redo, dispose);
        }

        public async void Replace(int left, int right) {
            Copyable paste = await Copyable.DecodeClipboard();

            if (paste != null && Copyable_Insert(paste, right, out Action undo, out Action redo, out Action dispose)) {
                Region_Delete(left, right, out Action undo2, out Action redo2, out Action dispose2);

                List<int> path = Track.GetPath(_chain);

                Program.Project.Undo.Add("Device Replaced",
                    undo2 + undo,
                    redo + redo2 + (() => {
                        Chain chain = ((Chain)Track.TraversePath(path));

                        Track.Get(chain).Window?.Selection.Select(chain[left + paste.Contents.Count - 1], true);
                    }),
                    dispose2 + dispose
                );
                
                Track.Get(_chain).Window?.Selection.Select(_chain[left + paste.Contents.Count - 1], true);
            }
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
            
                Track.Get(chain).Window?.Selection.Select(chain[right + 1], true);
            });

            for (int i = 0; i <= right - left; i++)
                _chain.Insert(right + i + 1, _chain[left + i].Clone());
            
            Track.Get(_chain).Window?.Selection.Select(_chain[right + 1], true);
        }

        public void Delete(int left, int right) {
            Region_Delete(left, right, out Action undo, out Action redo, out Action dispose);
            Program.Project.Undo.Add($"Device Removed", undo, redo, dispose);
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

            Chain init = ((Group)_chain.Devices[index])[0].Clone();

            List<int> path = Track.GetPath(_chain);

            Program.Project.Undo.Add($"Device Ungrouped", () => {
                Chain chain = ((Chain)Track.TraversePath(path));

                for (int i = index + init.Count - 1; i >= index; i--)
                    chain.Remove(i);
                
                chain.Insert(index, new Group(new List<Chain>() {init.Clone()}) {Expanded = 0});

            }, () => {
                Chain chain = ((Chain)Track.TraversePath(path));
                
                chain.Remove(index);
            
                for (int i = 0; i < init.Count; i++)
                    chain.Insert(index + i, init[i].Clone());

                Track.Get(chain).Window?.Selection.Select(chain[index], true);
                
            }, () => {
                init.Dispose();
            });

            _chain.Remove(index);
            
            for (int i = 0; i < init.Count; i++)
                _chain.Insert(index + i, init[i].Clone());

            Track.Get(_chain).Window?.Selection.Select(_chain[index], true);
        }
        
        public void Choke(int left, int right) {
            Chain init = new Chain();

            for (int i = left; i <= right; i++)
                init.Add(_chain.Devices[i].Clone());

            List<int> path = Track.GetPath(_chain);

            Program.Project.Undo.Add($"Device Choked", () => {
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
                
                chain.Insert(left, new Choke(1, init.Clone()));
            
            }, () => {
                init.Dispose();
            });
            
            for (int i = right; i >= left; i--)
                _chain.Remove(i);

            _chain.Insert(left, new Choke(1, init.Clone()));
        }

        public void Unchoke(int index) {
            if (_chain.Devices[index].GetType() != typeof(Choke)) return;

            Chain init = ((Choke)_chain.Devices[index]).Chain.Clone();

            List<int> path = Track.GetPath(_chain);

            Program.Project.Undo.Add($"Device Unchoked", () => {
                Chain chain = ((Chain)Track.TraversePath(path));

                for (int i = index + init.Count - 1; i >= index; i--)
                    chain.Remove(i);
                
                chain.Insert(index, new Choke(1, init.Clone()));

            }, () => {
                Chain chain = ((Chain)Track.TraversePath(path));
                
                chain.Remove(index);
            
                for (int i = 0; i < init.Count; i++)
                    chain.Insert(index + i, init[i].Clone());

                Track.Get(chain).Window?.Selection.Select(chain[index], true);
                
            }, () => {
                init.Dispose();
            });

            _chain.Remove(index);
            
            for (int i = 0; i < init.Count; i++)
                _chain.Insert(index + i, init[i].Clone());

            Track.Get(_chain).Window?.Selection.Select(_chain[index], true);
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
        
            Copyable loaded = await Copyable.DecodeFile(path, sender);
            
            if (loaded != null && Copyable_Insert(loaded, right, out Action undo, out Action redo, out Action dispose))
                Program.Project.Undo.Add("Device Imported", undo, redo, dispose);
        }
    }
}
