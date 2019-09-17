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
using Apollo.Viewers;
using Apollo.Windows;

namespace Apollo.DeviceViewers {
    public class GroupViewer: UserControl, IMultipleChainParentViewer, ISelectParentViewer {
        public static readonly string DeviceIdentifier = "group";

        public int? IExpanded {
            get => _group.Expanded;
        }

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Contents = this.Get<StackPanel>("Contents").Children;
            ChainAdd = this.Get<VerticalAdd>("ChainAdd");
        }
        
        Group _group;
        DeviceViewer _parent;
        Controls _root;

        ContextMenu ChainContextMenu;

        Controls Contents;
        VerticalAdd ChainAdd;

        void SetAlwaysShowing() {
            ChainAdd.AlwaysShowing = (Contents.Count == 1);

            for (int i = 1; i < Contents.Count; i++)
                ((ChainInfo)Contents[i]).ChainAdd.AlwaysShowing = false;

            if (Contents.Count > 1) ((ChainInfo)Contents.Last()).ChainAdd.AlwaysShowing = true;
        }

        public void Contents_Insert(int index, Chain chain) {
            ChainInfo viewer = new ChainInfo(chain);
            viewer.ChainAdded += Chain_Insert;
            viewer.ChainExpanded += Expand;
            chain.Info = viewer;

            Contents.Insert(index + 1, viewer);
            SetAlwaysShowing();

            if (IsArrangeValid && _group.Expanded != null && index <= _group.Expanded) _group.Expanded++;
        }

        public void Contents_Remove(int index) {
            if (IsArrangeValid && _group.Expanded != null) {
                if (index < _group.Expanded) _group.Expanded--;
                else if (index == _group.Expanded) Expand(null);
            }

            _group[index].Info = null;
            Contents.RemoveAt(index + 1);
            SetAlwaysShowing();
        }

        public GroupViewer() => new InvalidOperationException();

        public GroupViewer(Group group, DeviceViewer parent) {
            InitializeComponent();

            _group = group;
            _parent = parent;
            _root = _parent.Root.Children;

            ChainContextMenu = (ContextMenu)this.Resources["ChainContextMenu"];
            ChainContextMenu.AddHandler(MenuItem.ClickEvent, ChainContextMenu_Click);

            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);
            
            for (int i = 0; i < _group.Count; i++) {
                _group[i].ClearParentIndexChanged();
                Contents_Insert(i, _group[i]);
            }

            if (_group.Expanded != null) Expand_Insert(_group.Expanded.Value);
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            this.RemoveHandler(DragDrop.DropEvent, Drop);
            this.RemoveHandler(DragDrop.DragOverEvent, DragOver);

            ChainContextMenu.RemoveHandler(MenuItem.ClickEvent, ChainContextMenu_Click);
            ChainContextMenu = null;

            _group = null;
            _parent = null;
            _root = null;
        }

        void Expand_Insert(int index) {
            _root.Insert(1, new ChainViewer(_group[index], true));
            _root.Insert(2, new DeviceTail(_group, _parent));

            _parent.Border.CornerRadius = new CornerRadius(5, 0, 0, 5);
            _parent.Header.CornerRadius = new CornerRadius(5, 0, 0, 0);
            ((ChainInfo)Contents[index + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Bold;
        }

        void Expand_Remove() {
            _root.RemoveAt(2);
            _root.RemoveAt(1);

            _parent.Border.CornerRadius = new CornerRadius(5);
            _parent.Header.CornerRadius = new CornerRadius(5, 5, 0, 0);
            ((ChainInfo)Contents[_group.Expanded.Value + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Normal;
        }

        public void Expand(int? index) {
            if (_group.Expanded != null) {
                Expand_Remove();

                if (index == _group.Expanded) {
                    _group.Expanded = null;
                    return;
                }
            }

            if (index != null) Expand_Insert(index.Value);
            
            _group.Expanded = index;
        }

        void Chain_Insert(int index) {
            Chain chain = new Chain();
            if (Preferences.AutoCreatePageFilter) chain.Add(new PageFilter());
            if (Preferences.AutoCreateKeyFilter) chain.Add(new KeyFilter());
            if (Preferences.AutoCreatePattern) chain.Add(new Pattern());

            Chain_Insert(index, chain);
        }

        void Chain_InsertStart() => Chain_Insert(0);

        void Chain_Insert(int index, Chain chain) {
            Chain r = chain.Clone();
            List<int> path = Track.GetPath(_group);

            Program.Project.Undo.Add($"Group Chain {index + 1} Inserted", () => {
                ((Group)Track.TraversePath(path)).Remove(index);
            }, () => {
                ((Group)Track.TraversePath(path)).Insert(index, r.Clone());
            }, () => {
                r.Dispose();
            });

            _group.Insert(index, chain);
        }

        void Chain_Action(string action) => Chain_Action(action, false);
        void Chain_Action(string action, bool right) => Track.Get(_group)?.Window?.Selection.Action(action, _group, (right? _group.Count : 0) - 1);

        void ChainContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Chain_Action((string)((MenuItem)item).Header, true);
        }

        void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Right)
                ChainContextMenu.Open((Control)sender);

            e.Handled = true;
        }

        void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.Contains("chain") && !e.Data.Contains("device") && !e.Data.Contains(DataFormats.FileNames)) e.DragEffects = DragDropEffects.None; 
        }

        void Drop(object sender, DragEventArgs e) {
            e.Handled = true;
            
            IControl source = (IControl)e.Source;
            while (source.Name != "DropZoneAfter" && source.Name != "ChainAdd") {
                source = source.Parent;
                
                if (source == this) {
                    e.Handled = false;
                    return;
                }
            }

            int after = (source.Name == "DropZoneAfter")? _group.Count - 1 : -1;

            if (e.Data.Contains(DataFormats.FileNames)) {
                string path = e.Data.GetFileNames().FirstOrDefault();

                if (path != null) Import(after, path);

                return;
            }

            bool copy = e.Modifiers.HasFlag(App.ControlInput);
            bool result;

            if (e.Data.Contains("chain")) {
                List<Chain> moving = ((List<ISelect>)e.Data.Get("chain")).Select(i => (Chain)i).ToList();

                IMultipleChainParent source_parent = (IMultipleChainParent)moving[0].Parent;

                int before = moving[0].IParentIndex.Value - 1;

                if (result = Chain.Move(moving, _group, after, copy)) {
                    int before_pos = before;
                    int after_pos = moving[0].IParentIndex.Value - 1;
                    int count = moving.Count;

                    if (source_parent == _group && after < before)
                        before_pos += count;
                    
                    List<int> sourcepath = Track.GetPath((ISelect)source_parent);
                    List<int> targetpath = Track.GetPath((ISelect)_group);
                    
                    Program.Project.Undo.Add($"Chain {(copy? "Copied" : "Moved")}", copy
                        ? new Action(() => {
                            IMultipleChainParent targetdevice = ((IMultipleChainParent)Track.TraversePath(targetpath));

                            for (int i = after + count; i > after; i--)
                                targetdevice.Remove(i);

                        }) : new Action(() => {
                            IMultipleChainParent sourcedevice = ((IMultipleChainParent)Track.TraversePath(sourcepath));
                            IMultipleChainParent targetdevice = ((IMultipleChainParent)Track.TraversePath(targetpath));

                            List<Chain> umoving = (from i in Enumerable.Range(after_pos + 1, count) select targetdevice[i]).ToList();

                            Chain.Move(umoving, sourcedevice, before_pos, copy);

                    }), () => {
                        IMultipleChainParent sourcedevice = ((IMultipleChainParent)Track.TraversePath(sourcepath));
                        IMultipleChainParent targetdevice = ((IMultipleChainParent)Track.TraversePath(targetpath));

                        List<Chain> rmoving = (from i in Enumerable.Range(before + 1, count) select sourcedevice[i]).ToList();

                        Chain.Move(rmoving, targetdevice, after);
                    });
                }
            
            } else if (e.Data.Contains("device")) {
                List<Device> moving = ((List<ISelect>)e.Data.Get("device")).Select(i => (Device)i).ToList();

                Chain source_chain = moving[0].Parent;
                Chain target_chain;

                int before = moving[0].IParentIndex.Value - 1;
                after = -1;

                int remove = 0;

                if (source.Name != "DropZoneAfter") {
                    _group.Insert(remove = 0);
                    target_chain = _group[0];
                } else {
                    _group.Insert(remove = _group.Count);
                    target_chain = _group.Chains.Last();
                }

                if (result = Device.Move(moving, target_chain, after = target_chain.Count - 1, copy)) {
                    int before_pos = before;
                    int after_pos = moving[0].IParentIndex.Value - 1;
                    int count = moving.Count;

                    if (source_chain == target_chain && after < before)
                        before_pos += count;
                    
                    List<int> sourcepath = Track.GetPath(source_chain);
                    List<int> targetpath = Track.GetPath(target_chain);
                    
                    Program.Project.Undo.Add($"Device {(copy? "Copied" : "Moved")}", copy
                        ? new Action(() => {
                            Chain targetchain = ((Chain)Track.TraversePath(targetpath));

                            for (int i = after + count; i > after; i--)
                                targetchain.Remove(i);
                            
                            ((IMultipleChainParent)targetchain.Parent).Remove(remove);

                        }) : new Action(() => {
                            Chain sourcechain = ((Chain)Track.TraversePath(sourcepath));
                            Chain targetchain = ((Chain)Track.TraversePath(targetpath));

                            List<Device> umoving = (from i in Enumerable.Range(after_pos + 1, count) select targetchain[i]).ToList();

                            Device.Move(umoving, sourcechain, before_pos);

                            ((IMultipleChainParent)targetchain.Parent).Remove(remove);

                    }), () => {
                        Chain sourcechain = ((Chain)Track.TraversePath(sourcepath));
                        Chain targetchain;

                        IMultipleChainParent target = ((IMultipleChainParent)Track.TraversePath(targetpath.Skip(1).ToList()));
                        target.Insert(remove);
                        targetchain = target[remove];

                        List<Device> rmoving = (from i in Enumerable.Range(before + 1, count) select sourcechain[i]).ToList();

                        Device.Move(rmoving, targetchain, after, copy);
                    });
                    
                } else _group.Remove(remove);

            } else return;

            if (!result) e.DragEffects = DragDropEffects.None;
        }

        void Copyable_Insert(Copyable paste, int right, bool imported) {
            List<Chain> pasted;
            try {
                pasted = paste.Contents.Cast<Chain>().ToList();
            } catch (InvalidCastException) {
                return;
            }

            List<int> path = Track.GetPath(_group);

            Program.Project.Undo.Add($"Chain {(imported? "Imported" : "Pasted")}", () => {
                Group group = ((Group)Track.TraversePath(path));

                for (int i = paste.Contents.Count - 1; i >= 0; i--)
                    group.Remove(right + i + 1);

            }, () => {
                Group group = ((Group)Track.TraversePath(path));

                for (int i = 0; i < paste.Contents.Count; i++)
                    group.Insert(right + i + 1, pasted[i].Clone());
            
            }, () => {
                foreach (Chain chain in pasted) chain.Dispose();
                pasted = null;
            });

            for (int i = 0; i < paste.Contents.Count; i++)
                _group.Insert(right + i + 1, pasted[i].Clone());
        }

        public async void Copy(int left, int right, bool cut = false) {
            Copyable copy = new Copyable();
            
            for (int i = left; i <= right; i++)
                copy.Contents.Add(_group[i]);

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
            List<int> path = Track.GetPath(_group);

            Program.Project.Undo.Add($"Chain Duplicated", () => {
                Group group = ((Group)Track.TraversePath(path));

                for (int i = right - left; i >= 0; i--)
                    group.Remove(right + i + 1);

            }, () => {
                Group group = ((Group)Track.TraversePath(path));

                for (int i = 0; i <= right - left; i++)
                    group.Insert(right + i + 1, group[left + i].Clone());
            });

            for (int i = 0; i <= right - left; i++)
                _group.Insert(right + i + 1, _group[left + i].Clone());
        }

        public void Delete(int left, int right) {
            List<Chain> u = (from i in Enumerable.Range(left, right - left + 1) select _group[i].Clone()).ToList();

            List<int> path = Track.GetPath(_group);

            Program.Project.Undo.Add($"Chain Removed", () => {
                Group group = ((Group)Track.TraversePath(path));

                for (int i = left; i <= right; i++)
                    group.Insert(i, u[i - left].Clone());

            }, () => {
                Group group = ((Group)Track.TraversePath(path));

                for (int i = right; i >= left; i--)
                    group.Remove(i);

            }, () => {
                foreach (Chain chain in u) chain.Dispose();
                u = null;
            });

            for (int i = right; i >= left; i--)
                _group.Remove(i);
        }

        public void Group(int left, int right) {}
        public void Ungroup(int index) {}
        
        public void Mute(int left, int right) {
            List<bool> u = (from i in Enumerable.Range(left, right - left + 1) select _group[i].Enabled).ToList();
            bool r = !_group[left].Enabled;

            List<int> path = Track.GetPath(_group);

            Program.Project.Undo.Add($"Chain Muted", () => {
                Group group = ((Group)Track.TraversePath(path));

                for (int i = left; i <= right; i++)
                    group[i].Enabled = u[i - left];

            }, () => {
                Group group = ((Group)Track.TraversePath(path));

                for (int i = left; i <= right; i++)
                    group[i].Enabled = r;
            });

            for (int i = left; i <= right; i++)
                _group[i].Enabled = r;
        }
        
        public void Rename(int left, int right) => ((ChainInfo)Contents[left + 1]).StartInput(left, right);

        public async void Export(int left, int right) {
            Window sender = Track.Get(_group).Window;

            SaveFileDialog sfd = new SaveFileDialog() {
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                        Extensions = new List<string>() {
                            "apchn"
                        },
                        Name = "Apollo Chain Preset"
                    }
                },
                Title = "Export Chain Preset"
            };
            
            string result = await sfd.ShowAsync(sender);

            if (result != null) {
                string[] file = result.Split(Path.DirectorySeparatorChar);

                if (Directory.Exists(string.Join("/", file.Take(file.Count() - 1)))) {
                    Copyable copy = new Copyable();
                    
                    for (int i = left; i <= right; i++)
                        copy.Contents.Add(_group[i]);

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
            Window sender = Track.Get(_group).Window;

            if (path == null) {
                OpenFileDialog ofd = new OpenFileDialog() {
                    AllowMultiple = false,
                    Filters = new List<FileDialogFilter>() {
                        new FileDialogFilter() {
                            Extensions = new List<string>() {
                                "apchn"
                            },
                            Name = "Apollo Chain Preset"
                        }
                    },
                    Title = "Import Chain Preset"
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
