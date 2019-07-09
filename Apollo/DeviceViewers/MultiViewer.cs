﻿using System;
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
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Interfaces;
using Apollo.Viewers;
using Apollo.Windows;

namespace Apollo.DeviceViewers {
    public class MultiViewer: UserControl, IMultipleChainParentViewer, ISelectParentViewer {
        public static readonly string DeviceIdentifier = "multi";

        public int? IExpanded {
            get => _multi.Expanded;
        }

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            MultiMode = this.Get<ComboBox>("MultiMode");
            
            Contents = this.Get<StackPanel>("Contents").Children;
            ChainAdd = this.Get<VerticalAdd>("ChainAdd");
        }
        
        Multi _multi;
        DeviceViewer _parent;
        Controls _root;

        ContextMenu ChainContextMenu;

        Controls Contents;
        ComboBox MultiMode;
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

            if (IsArrangeValid && _multi.Expanded != null && index <= _multi.Expanded) _multi.Expanded++;
        }

        public void Contents_Remove(int index) {
            if (IsArrangeValid && _multi.Expanded != null) {
                if (index < _multi.Expanded) _multi.Expanded--;
                else if (index == _multi.Expanded) Expand(null);
            }

            _multi[index].Info = null;
            Contents.RemoveAt(index + 1);
            SetAlwaysShowing();
        }

        public MultiViewer(Multi multi, DeviceViewer parent) {
            InitializeComponent();

            _multi = multi;
            _multi.Preprocess.ClearParentIndexChanged();

            _parent = parent;
            _parent.Border.CornerRadius = new CornerRadius(0, 5, 5, 0);
            _parent.Header.CornerRadius = new CornerRadius(0, 5, 0, 0);

            _root = _parent.Root.Children;
            _root.Insert(0, new DeviceHead(_multi, parent));
            _root.Insert(1, new ChainViewer(_multi.Preprocess, true));

            MultiMode.SelectedIndex = (int)_multi.Mode;

            ChainContextMenu = (ContextMenu)this.Resources["ChainContextMenu"];
            ChainContextMenu.AddHandler(MenuItem.ClickEvent, ChainContextMenu_Click);

            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);
            
            for (int i = 0; i < _multi.Count; i++) {
                _multi[i].ClearParentIndexChanged();
                Contents_Insert(i, _multi[i]);
            }

            if (_multi.Expanded != null) Expand_Insert(multi.Expanded.Value);
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            this.RemoveHandler(DragDrop.DropEvent, Drop);
            this.RemoveHandler(DragDrop.DragOverEvent, DragOver);

            ChainContextMenu.RemoveHandler(MenuItem.ClickEvent, ChainContextMenu_Click);
            ChainContextMenu = null;

            _multi = null;
            _parent = null;
            _root = null;
        }

        void Expand_Insert(int index) {
            _root.Insert(3, new ChainViewer(_multi[index], true));
            _root.Insert(4, new DeviceTail(_multi, _parent));

            _parent.Border.CornerRadius = new CornerRadius(0);
            _parent.Header.CornerRadius = new CornerRadius(0);
            ((ChainInfo)Contents[index + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Bold;
        }

        void Expand_Remove() {
            _root.RemoveAt(4);
            _root.RemoveAt(3);

            _parent.Border.CornerRadius = new CornerRadius(0, 5, 5, 0);
            _parent.Header.CornerRadius = new CornerRadius(0, 5, 0, 0);
            ((ChainInfo)Contents[_multi.Expanded.Value + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Normal;
        }

        public void Expand(int? index) {
            if (_multi.Expanded != null) {
                Expand_Remove();

                if (index == _multi.Expanded) {
                    _multi.Expanded = null;
                    return;
                }
            }

            if (index != null) Expand_Insert(index.Value);
            
            _multi.Expanded = index;
        }

        void Chain_Insert(int index) => Chain_Insert(index, new Chain());
        void Chain_InsertStart() => Chain_Insert(0);

        void Chain_Insert(int index, Chain chain) {
            Chain r = chain.Clone();
            List<int> path = Track.GetPath(_multi);

            Program.Project.Undo.Add($"Multi Chain {index + 1} Inserted", () => {
                ((Multi)Track.TraversePath(path)).Remove(index);
            }, () => {
                ((Multi)Track.TraversePath(path)).Insert(index, r.Clone());
            }, () => {
                r.Dispose();
            });

            _multi.Insert(index, chain);
        }

        void Chain_Action(string action) => Chain_Action(action, false);
        void Chain_Action(string action, bool right) => Track.Get(_multi)?.Window?.Selection.Action(action, _multi, (right? _multi.Count : 0) - 1);

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

        void Mode_Changed(object sender, SelectionChangedEventArgs e) {
            MultiType selected = (MultiType)MultiMode.SelectedIndex;

            if (_multi.Mode != selected) {
                MultiType u = _multi.Mode;
                MultiType r = selected;
                List<int> path = Track.GetPath(_multi);

                Program.Project.Undo.Add($"Direction Changed to {((ComboBoxItem)MultiMode.ItemContainerGenerator.ContainerFromIndex((int)r)).Content}", () => {
                    ((Multi)Track.TraversePath(path)).Mode = u;
                }, () => {
                    ((Multi)Track.TraversePath(path)).Mode = r;
                });

                _multi.Mode = selected;
            }
        }

        public void SetMode(MultiType mode) => MultiMode.SelectedIndex = (int)mode;

        void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.Contains("chain") && !e.Data.Contains("device")) e.DragEffects = DragDropEffects.None;
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

            bool copy = e.Modifiers.HasFlag(InputModifiers.Control);
            bool result;

            if (e.Data.Contains("chain")) {
                List<Chain> moving = ((List<ISelect>)e.Data.Get("chain")).Select(i => (Chain)i).ToList();

                IMultipleChainParent source_parent = (IMultipleChainParent)moving[0].Parent;

                int before = moving[0].IParentIndex.Value - 1;
                int after = (source.Name == "DropZoneAfter")? _multi.Count - 1 : -1;

                if (result = Chain.Move(moving, _multi, after, copy)) {
                    int before_pos = before;
                    int after_pos = moving[0].IParentIndex.Value - 1;
                    int count = moving.Count;

                    if (source_parent == _multi && after < before)
                        before_pos += count;
                    
                    List<int> sourcepath = Track.GetPath((ISelect)source_parent);
                    List<int> targetpath = Track.GetPath((ISelect)_multi);
                    
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
                int after = -1;

                int remove = 0;

                if (source.Name != "DropZoneAfter") {
                    _multi.Insert(remove = 0);
                    target_chain = _multi[0];
                } else {
                    _multi.Insert(remove = _multi.Count);
                    target_chain = _multi.Chains.Last();
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

                } else _multi.Remove(remove);

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
            
            List<int> path = Track.GetPath(_multi);

            Program.Project.Undo.Add($"Chain {(imported? "Imported" : "Pasted")}", () => {
                Multi multi = ((Multi)Track.TraversePath(path));

                for (int i = paste.Contents.Count - 1; i >= 0; i--)
                    multi.Remove(right + i + 1);

            }, () => {
                Multi multi = ((Multi)Track.TraversePath(path));

                for (int i = 0; i < paste.Contents.Count; i++)
                    multi.Insert(right + i + 1, pasted[i].Clone());

            }, () => {
                foreach (Chain chain in pasted) chain.Dispose();      
                pasted = null;
            });

            for (int i = 0; i < paste.Contents.Count; i++)
                _multi.Insert(right + i + 1, pasted[i].Clone());
        }

        public async void Copy(int left, int right, bool cut = false) {
            Copyable copy = new Copyable();
            
            for (int i = left; i <= right; i++)
                copy.Contents.Add(_multi[i]);

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
            List<int> path = Track.GetPath(_multi);

            Program.Project.Undo.Add($"Chain Duplicated", () => {
                Multi multi = ((Multi)Track.TraversePath(path));

                for (int i = right - left; i >= 0; i--)
                    multi.Remove(right + i + 1);

            }, () => {
                Multi multi = ((Multi)Track.TraversePath(path));

                for (int i = 0; i <= right - left; i++)
                    multi.Insert(right + i + 1, multi[left + i].Clone());
            });

            for (int i = 0; i <= right - left; i++)
                _multi.Insert(right + i + 1, _multi[left + i].Clone());
        }

        public void Delete(int left, int right) {
            List<Chain> u = (from i in Enumerable.Range(left, right - left + 1) select _multi[i].Clone()).ToList();

            List<int> path = Track.GetPath(_multi);

            Program.Project.Undo.Add($"Chain Removed", () => {
                Multi multi = ((Multi)Track.TraversePath(path));

                for (int i = left; i <= right; i++)
                    multi.Insert(i, u[i - left].Clone());

            }, () => {
                Multi multi = ((Multi)Track.TraversePath(path));

                for (int i = right; i >= left; i--)
                    multi.Remove(i);

            }, () => {
                foreach (Chain chain in u) chain.Dispose();
                u = null;
            });

            for (int i = right; i >= left; i--)
                _multi.Remove(i);
        }

        public void Group(int left, int right) {}
        public void Ungroup(int index) {}
        
        public void Mute(int left, int right) {
            List<bool> u = (from i in Enumerable.Range(left, right - left + 1) select _multi[i].Enabled).ToList();
            bool r = !_multi[left].Enabled;

            List<int> path = Track.GetPath(_multi);

            Program.Project.Undo.Add($"Chain Muted", () => {
                Multi multi = ((Multi)Track.TraversePath(path));

                for (int i = left; i <= right; i++)
                    multi[i].Enabled = u[i - left];

            }, () => {
                Multi multi = ((Multi)Track.TraversePath(path));

                for (int i = left; i <= right; i++)
                    multi[i].Enabled = r;
            });

            for (int i = left; i <= right; i++)
                _multi[i].Enabled = r;
        }

        public void Rename(int left, int right) => ((ChainInfo)Contents[left + 1]).StartInput(left, right);

        public async void Export(int left, int right) {
            Window sender = Track.Get(_multi).Window;

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
                        copy.Contents.Add(_multi[i]);

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
        
        public async void Import(int right) {
            Window sender = Track.Get(_multi).Window;
            
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

            if (result.Length > 0) {
                Copyable loaded;

                try {
                    using (FileStream file = File.Open(result[0], FileMode.Open, FileAccess.Read))
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
}
