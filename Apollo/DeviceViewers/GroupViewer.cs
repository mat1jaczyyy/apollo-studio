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
using Apollo.Selection;
using Apollo.Viewers;
using Apollo.Windows;

namespace Apollo.DeviceViewers {
    public class GroupViewer: UserControl, ISelectParentViewer {
        public static readonly string DeviceIdentifier = "group";

        public int? IExpanded {
            get => _group.Expanded;
        }

        protected virtual void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Contents = this.Get<StackPanel>("Contents").Children;
            ChainAdd = this.Get<VerticalAdd>("ChainAdd");
        }
        
        protected Group _group;
        protected DeviceViewer _parent;
        protected Controls _root;

        protected Controls Contents;
        protected VerticalAdd ChainAdd;

        protected void SetAlwaysShowing() {
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

            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);
            
            for (int i = 0; i < _group.Count; i++) {
                _group[i].ClearParentIndexChanged();
                Contents_Insert(i, _group[i]);
            }

            if (_group.Expanded != null) Expand_Insert(_group.Expanded.Value);
        }

        protected void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            this.RemoveHandler(DragDrop.DropEvent, Drop);
            this.RemoveHandler(DragDrop.DragOverEvent, DragOver);

            _group = null;
            _parent = null;
            _root = null;
        }

        protected int ExpandBase = 1;

        protected virtual void Expand_Insert(int index) {
            _root.Insert(ExpandBase, new ChainViewer(_group[index], true));
            _root.Insert(ExpandBase + 1, new DeviceTail(_group, _parent));

            _parent.Border.CornerRadius = new CornerRadius(5, 0, 0, 5);
            _parent.Header.CornerRadius = new CornerRadius(5, 0, 0, 0);
            ((ChainInfo)Contents[index + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Bold;
        }

        protected virtual void Expand_Remove() {
            _root.RemoveAt(ExpandBase + 1);
            _root.RemoveAt(ExpandBase);

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

        protected void Chain_Insert(int index) {
            Chain chain = new Chain();

            if (this.GetType() == typeof(Group)) {
                if (Preferences.AutoCreateMacroFilter) chain.Add(new MacroFilter());
                if (Preferences.AutoCreateKeyFilter) chain.Add(new KeyFilter());
                if (Preferences.AutoCreatePattern) chain.Add(new Pattern());
            }

            Chain_Insert(index, chain);
        }

        protected void Chain_InsertStart() => Chain_Insert(0);

        protected void Chain_Insert(int index, Chain chain) {
            Chain r = chain.Clone();
            List<int> path = Track.GetPath(_group);

            Program.Project.Undo.Add($"Group Chain {index + 1} Inserted", () => {   // Multi also uses this
                Track.TraversePath<Group>(path).Remove(index);
            }, () => {
                Track.TraversePath<Group>(path).Insert(index, r.Clone());
            }, () => {
                r.Dispose();
            });

            _group.Insert(index, chain);
        }

        protected void Chain_Action(string action) => Chain_Action(action, false);
        protected void Chain_Action(string action, bool right) => Track.Get(_group)?.Window?.Selection.Action(action, _group, (right? _group.Count : 0) - 1);

        protected void ContextMenu_Action(string action) => Chain_Action(action, true);

        protected void Click(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.RightButtonReleased)
                ((ApolloContextMenu)this.Resources["ChainContextMenu"]).Open((Control)sender);

            e.Handled = true;
        }

        protected void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.Contains("chain") && !e.Data.Contains("device") && !e.Data.Contains(DataFormats.FileNames)) e.DragEffects = DragDropEffects.None; 
        }

        protected void Drop(object sender, DragEventArgs e) {
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

                Group source_parent = (Group)moving[0].Parent;

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
                            Group targetdevice = Track.TraversePath<Group>(targetpath);

                            for (int i = after + count; i > after; i--)
                                targetdevice.Remove(i);

                        }) : new Action(() => {
                            Group sourcedevice = Track.TraversePath<Group>(sourcepath);
                            Group targetdevice = Track.TraversePath<Group>(targetpath);

                            List<Chain> umoving = (from i in Enumerable.Range(after_pos + 1, count) select targetdevice[i]).ToList();

                            Chain.Move(umoving, sourcedevice, before_pos);

                    }), () => {
                        Group sourcedevice = Track.TraversePath<Group>(sourcepath);
                        Group targetdevice = Track.TraversePath<Group>(targetpath);

                        List<Chain> rmoving = (from i in Enumerable.Range(before + 1, count) select sourcedevice[i]).ToList();

                        Chain.Move(rmoving, targetdevice, after, copy);
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
                            Chain targetchain = Track.TraversePath<Chain>(targetpath);

                            for (int i = after + count; i > after; i--)
                                targetchain.Remove(i);
                            
                            ((Group)targetchain.Parent).Remove(remove);

                        }) : new Action(() => {
                            Chain sourcechain = Track.TraversePath<Chain>(sourcepath);
                            Chain targetchain = Track.TraversePath<Chain>(targetpath);

                            List<Device> umoving = (from i in Enumerable.Range(after_pos + 1, count) select targetchain[i]).ToList();

                            Device.Move(umoving, sourcechain, before_pos);

                            ((Group)targetchain.Parent).Remove(remove);

                    }), () => {
                        Chain sourcechain = Track.TraversePath<Chain>(sourcepath);
                        Chain targetchain;

                        Group target = Track.TraversePath<Group>(targetpath.Skip(1).ToList());
                        target.Insert(remove);
                        targetchain = target[remove];

                        List<Device> rmoving = (from i in Enumerable.Range(before + 1, count) select sourcechain[i]).ToList();

                        Device.Move(rmoving, targetchain, after, copy);
                    });
                    
                } else _group.Remove(remove);

            } else return;

            if (!result) e.DragEffects = DragDropEffects.None;
        }

        protected bool Copyable_Insert(Copyable paste, int right, out Action undo, out Action redo, out Action dispose) {
            undo = redo = dispose = null;

            List<Chain> pasted;
            try {
                pasted = paste.Contents.Cast<Chain>().ToList();
            } catch (InvalidCastException) {
                return false;
            }

            List<int> path = Track.GetPath(_group);

            undo = () => {
                Group group = Track.TraversePath<Group>(path);

                for (int i = paste.Contents.Count - 1; i >= 0; i--)
                    group.Remove(right + i + 1);
            };
            
            redo = () => {
                Group group = Track.TraversePath<Group>(path);

                for (int i = 0; i < paste.Contents.Count; i++)
                    group.Insert(right + i + 1, pasted[i].Clone());
            
                Track.Get(group).Window?.Selection.Select(group[right + 1], true);
            };
            
            dispose = () => {
                foreach (Chain chain in pasted) chain.Dispose();
                pasted = null;
            };

            for (int i = 0; i < paste.Contents.Count; i++)
                _group.Insert(right + i + 1, pasted[i].Clone());
            
            Track.Get(_group).Window?.Selection.Select(_group[right + 1], true);
            
            return true;
        }

        protected void Region_Delete(int left, int right, out Action undo, out Action redo, out Action dispose) {
            List<Chain> u = (from i in Enumerable.Range(left, right - left + 1) select _group[i].Clone()).ToList();

            List<int> path = Track.GetPath(_group);

            undo = () => {
                Group group = Track.TraversePath<Group>(path);

                for (int i = left; i <= right; i++)
                    group.Insert(i, u[i - left].Clone());
            };
            
            redo = () => {
                Group group = Track.TraversePath<Group>(path);

                for (int i = right; i >= left; i--)
                    group.Remove(i);
            };
            
            dispose = () => {
                foreach (Chain chain in u) chain.Dispose();
                u = null;
            };

            for (int i = right; i >= left; i--)
                _group.Remove(i);
        }

        public void Copy(int left, int right, bool cut = false) {
            Copyable copy = new Copyable();
            
            for (int i = left; i <= right; i++)
                copy.Contents.Add(_group[i]);

            copy.StoreToClipboard();

            if (cut) Delete(left, right);
        }

        public async void Paste(int right) {            
            Copyable paste = await Copyable.DecodeClipboard();

            if (paste != null && Copyable_Insert(paste, right, out Action undo, out Action redo, out Action dispose))
                Program.Project.Undo.Add("Chain Pasted", undo, redo, dispose);   // Multi also uses this
        }

        public async void Replace(int left, int right) {
            Copyable paste = await Copyable.DecodeClipboard();

            if (paste != null && Copyable_Insert(paste, right, out Action undo, out Action redo, out Action dispose)) {
                Region_Delete(left, right, out Action undo2, out Action redo2, out Action dispose2);

                List<int> path = Track.GetPath(_group);

                Program.Project.Undo.Add("Chain Replaced",   // Multi also uses this
                    undo2 + undo,
                    redo + redo2 + (() => {
                        Group group = Track.TraversePath<Group>(path);

                        Track.Get(group).Window?.Selection.Select(group[left + paste.Contents.Count - 1], true);
                    }),
                    dispose2 + dispose
                );
                
                Track.Get(_group).Window?.Selection.Select(_group[left + paste.Contents.Count - 1], true);
            }
        }

        public void Duplicate(int left, int right) {   // Multi also uses this
            List<int> path = Track.GetPath(_group);

            Program.Project.Undo.Add($"Chain Duplicated", () => {
                Group group = Track.TraversePath<Group>(path);

                for (int i = right - left; i >= 0; i--)
                    group.Remove(right + i + 1);

            }, () => {
                Group group = Track.TraversePath<Group>(path);

                for (int i = 0; i <= right - left; i++)
                    group.Insert(right + i + 1, group[left + i].Clone());
            
                Track.Get(group).Window?.Selection.Select(group[right + 1], true);
            });

            for (int i = 0; i <= right - left; i++)
                _group.Insert(right + i + 1, _group[left + i].Clone());
            
            Track.Get(_group).Window?.Selection.Select(_group[right + 1], true);
        }

        public void Delete(int left, int right) {
            Region_Delete(left, right, out Action undo, out Action redo, out Action dispose);
            Program.Project.Undo.Add($"Chain Removed", undo, redo, dispose);   // Multi also uses this
        }

        public void Group(int left, int right) {}
        public void Ungroup(int index) {}
        public void Choke(int left, int right) {}
        public void Unchoke(int index) {}
        
        public void Mute(int left, int right) {
            List<bool> u = (from i in Enumerable.Range(left, right - left + 1) select _group[i].Enabled).ToList();
            bool r = !_group[left].Enabled;

            List<int> path = Track.GetPath(_group);

            Program.Project.Undo.Add($"Chain Muted", () => {      // Multi also uses this
                Group group = Track.TraversePath<Group>(path);

                for (int i = left; i <= right; i++)
                    group[i].Enabled = u[i - left];

            }, () => {
                Group group = Track.TraversePath<Group>(path);

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

            Copyable loaded = await Copyable.DecodeFile(path, sender);
            
            if (loaded != null && Copyable_Insert(loaded, right, out Action undo, out Action redo, out Action dispose))
                Program.Project.Undo.Add("Chain Imported", undo, redo, dispose);
        }
    }
}
