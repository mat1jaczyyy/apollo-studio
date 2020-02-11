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

            Grid = this.Get<LaunchpadGrid>("Grid");
            GridContainer = this.Get<Border>("GridContainer");
        }
        
        Multi _multi;
        DeviceViewer _parent;
        Controls _root;

        Controls Contents;
        ComboBox MultiMode;
        VerticalAdd ChainAdd;

        LaunchpadGrid Grid;
        Border GridContainer;

        SolidColorBrush GetColor(bool value) => (SolidColorBrush)Application.Current.Styles.FindResource(value? "ThemeAccentBrush" : "ThemeForegroundLowBrush");

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

        public MultiViewer() => new InvalidOperationException();

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

            SetMode(_multi.Mode);

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

            _multi = null;
            _parent = null;
            _root = null;
        }

        void Expand_Insert(int index) {
            _root.Insert(3, new ChainViewer(_multi[index], true));
            _root.Insert(4, new DeviceTail(_multi, _parent));

            GridContainer.MaxWidth = double.MaxValue;
            Set(-1, _multi.GetFilter(index));

            _parent.Border.CornerRadius = new CornerRadius(0);
            _parent.Header.CornerRadius = new CornerRadius(0);
            ((ChainInfo)Contents[index + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Bold;
        }

        void Expand_Remove() {
            _root.RemoveAt(4);
            _root.RemoveAt(3);

            GridContainer.MaxWidth = 0;

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
                Track.TraversePath<Multi>(path).Remove(index);
            }, () => {
                Track.TraversePath<Multi>(path).Insert(index, r.Clone());
            }, () => {
                r.Dispose();
            });

            _multi.Insert(index, chain);
        }

        void Chain_Action(string action) => Chain_Action(action, false);
        void Chain_Action(string action, bool right) => Track.Get(_multi)?.Window?.Selection.Action(action, _multi, (right? _multi.Count : 0) - 1);

        void ContextMenu_Action(string action) => Chain_Action(action, true);

        void Click(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.RightButtonReleased)
                ((ApolloContextMenu)this.Resources["ChainContextMenu"]).Open((Control)sender);

            e.Handled = true;
        }

        void Mode_Changed(object sender, SelectionChangedEventArgs e) {
            MultiType selected = (MultiType)MultiMode.SelectedIndex;

            if (_multi.Mode != selected) {
                MultiType u = _multi.Mode;
                MultiType r = selected;
                List<int> path = Track.GetPath(_multi);

                Program.Project.Undo.Add($"Direction Changed to {((ComboBoxItem)MultiMode.ItemContainerGenerator.ContainerFromIndex((int)r)).Content}", () => {
                    Track.TraversePath<Multi>(path).Mode = u;
                }, () => {
                    Track.TraversePath<Multi>(path).Mode = r;
                });

                _multi.Mode = selected;
            }
        }

        public void SetMode(MultiType mode) {
            MultiMode.SelectedIndex = (int)mode;

            GridContainer.IsVisible = mode == MultiType.Key;
        }
    
        bool drawingState;
        bool[] old;

        void PadStarted(int index) {
            bool[] filter = _multi.GetFilter((int)_multi.Expanded);
            drawingState = !filter[LaunchpadGrid.GridToSignal(index)];
            old = filter.ToArray();
        }

        void PadPressed(int index) => Grid.SetColor(
            index,
            GetColor(_multi.GetFilter((int)_multi.Expanded)[LaunchpadGrid.GridToSignal(index)] = drawingState)
        );

        void PadFinished(int index) {
            if (old == null) return;

            bool[] u = old.ToArray();
            bool[] r = _multi.GetFilter((int)_multi.Expanded).ToArray();
            List<int> path = Track.GetPath(_multi);
            int selected = (int)_multi.Expanded;

            Program.Project.Undo.Add($"MultiFilter Changed", () => {
                Track.TraversePath<Multi>(path).SetFilter(selected, u.ToArray());
            }, () => {
                Track.TraversePath<Multi>(path).SetFilter(selected, r.ToArray());
            });

            old = null;
        }

        public void Set(int index, bool[] filter) {
            if (index != -1 && _multi.Expanded != index) return;

            for (int i = 0; i < 100; i++)
                Grid.SetColor(LaunchpadGrid.SignalToGrid(i), GetColor(filter[i]));
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
            
            int after = (source.Name == "DropZoneAfter")? _multi.Count - 1 : -1;

            if (e.Data.Contains(DataFormats.FileNames)) {
                string path = e.Data.GetFileNames().FirstOrDefault();

                if (path != null) Import(after, path);

                return;
            }

            bool copy = e.KeyModifiers.HasFlag(App.ControlKey);
            bool result;

            if (e.Data.Contains("chain")) {
                List<Chain> moving = ((List<ISelect>)e.Data.Get("chain")).Select(i => (Chain)i).ToList();

                IMultipleChainParent source_parent = (IMultipleChainParent)moving[0].Parent;

                int before = moving[0].IParentIndex.Value - 1;

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
                            IMultipleChainParent targetdevice = Track.TraversePath<IMultipleChainParent>(targetpath);

                            for (int i = after + count; i > after; i--)
                                targetdevice.Remove(i);

                        }) : new Action(() => {
                            IMultipleChainParent sourcedevice = Track.TraversePath<IMultipleChainParent>(sourcepath);
                            IMultipleChainParent targetdevice = Track.TraversePath<IMultipleChainParent>(targetpath);

                            List<Chain> umoving = (from i in Enumerable.Range(after_pos + 1, count) select targetdevice[i]).ToList();

                            Chain.Move(umoving, sourcedevice, before_pos);

                    }), () => {
                        IMultipleChainParent sourcedevice = Track.TraversePath<IMultipleChainParent>(sourcepath);
                        IMultipleChainParent targetdevice = Track.TraversePath<IMultipleChainParent>(targetpath);

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
                            Chain targetchain = Track.TraversePath<Chain>(targetpath);

                            for (int i = after + count; i > after; i--)
                                targetchain.Remove(i);
                            
                            ((IMultipleChainParent)targetchain.Parent).Remove(remove);

                        }) : new Action(() => {
                            Chain sourcechain = Track.TraversePath<Chain>(sourcepath);
                            Chain targetchain = Track.TraversePath<Chain>(targetpath);

                            List<Device> umoving = (from i in Enumerable.Range(after_pos + 1, count) select targetchain[i]).ToList();

                            Device.Move(umoving, sourcechain, before_pos);

                            ((IMultipleChainParent)targetchain.Parent).Remove(remove);

                    }), () => {
                        Chain sourcechain = Track.TraversePath<Chain>(sourcepath);
                        Chain targetchain;

                        IMultipleChainParent target = Track.TraversePath<IMultipleChainParent>(targetpath.Skip(1).ToList());
                        target.Insert(remove);
                        targetchain = target[remove];

                        List<Device> rmoving = (from i in Enumerable.Range(before + 1, count) select sourcechain[i]).ToList();

                        Device.Move(rmoving, targetchain, after, copy);
                    });

                } else _multi.Remove(remove);

            } else return;

            if (!result) e.DragEffects = DragDropEffects.None;
        }

        bool Copyable_Insert(Copyable paste, int right, out Action undo, out Action redo, out Action dispose) {
            undo = redo = dispose = null;

            List<Chain> pasted;
            try {
                pasted = paste.Contents.Cast<Chain>().ToList();
            } catch (InvalidCastException) {
                return false;
            }
            
            List<int> path = Track.GetPath(_multi);

            undo = () => {
                Multi multi = Track.TraversePath<Multi>(path);

                for (int i = paste.Contents.Count - 1; i >= 0; i--)
                    multi.Remove(right + i + 1);
            };
            
            redo = () => {
                Multi multi = Track.TraversePath<Multi>(path);

                for (int i = 0; i < paste.Contents.Count; i++)
                    multi.Insert(right + i + 1, pasted[i].Clone());
            
                Track.Get(multi).Window?.Selection.Select(multi[right + 1], true);
            };
            
            dispose = () => {
                foreach (Chain chain in pasted) chain.Dispose();      
                pasted = null;
            };

            for (int i = 0; i < paste.Contents.Count; i++)
                _multi.Insert(right + i + 1, pasted[i].Clone());
            
            Track.Get(_multi).Window?.Selection.Select(_multi[right + 1], true);
            
            return true;
        }

        void Region_Delete(int left, int right, out Action undo, out Action redo, out Action dispose) {
            List<Chain> u = (from i in Enumerable.Range(left, right - left + 1) select _multi[i].Clone()).ToList();

            List<int> path = Track.GetPath(_multi);

            undo = () => {
                Multi multi = Track.TraversePath<Multi>(path);

                for (int i = left; i <= right; i++)
                    multi.Insert(i, u[i - left].Clone());
            };
            
            redo = () => {
                Multi multi = Track.TraversePath<Multi>(path);

                for (int i = right; i >= left; i--)
                    multi.Remove(i);
            };
            
            dispose = () => {
                foreach (Chain chain in u) chain.Dispose();
                u = null;
            };

            for (int i = right; i >= left; i--)
                _multi.Remove(i);
        }

        public void Copy(int left, int right, bool cut = false) {
            Copyable copy = new Copyable();
            
            for (int i = left; i <= right; i++)
                copy.Contents.Add(_multi[i]);

            copy.StoreToClipboard();

            if (cut) Delete(left, right);
        }

        public async void Paste(int right) {
            Copyable paste = await Copyable.DecodeClipboard();

            if (paste != null && Copyable_Insert(paste, right, out Action undo, out Action redo, out Action dispose))
                Program.Project.Undo.Add("Chain Pasted", undo, redo, dispose);
        }

        public async void Replace(int left, int right) {
            Copyable paste = await Copyable.DecodeClipboard();

            if (paste != null && Copyable_Insert(paste, right, out Action undo, out Action redo, out Action dispose)) {
                Region_Delete(left, right, out Action undo2, out Action redo2, out Action dispose2);

                List<int> path = Track.GetPath(_multi);

                Program.Project.Undo.Add("Chain Replaced",
                    undo2 + undo,
                    redo + redo2 + (() => {
                        Multi multi = Track.TraversePath<Multi>(path);

                        Track.Get(multi).Window?.Selection.Select(multi[left + paste.Contents.Count - 1], true);
                    }),
                    dispose2 + dispose
                );
                
                Track.Get(_multi).Window?.Selection.Select(_multi[left + paste.Contents.Count - 1], true);
            }
        }

        public void Duplicate(int left, int right) {
            List<int> path = Track.GetPath(_multi);

            Program.Project.Undo.Add($"Chain Duplicated", () => {
                Multi multi = Track.TraversePath<Multi>(path);

                for (int i = right - left; i >= 0; i--)
                    multi.Remove(right + i + 1);

            }, () => {
                Multi multi = Track.TraversePath<Multi>(path);

                for (int i = 0; i <= right - left; i++)
                    multi.Insert(right + i + 1, multi[left + i].Clone());
            
                Track.Get(multi).Window?.Selection.Select(multi[right + 1], true);
            });

            for (int i = 0; i <= right - left; i++)
                _multi.Insert(right + i + 1, _multi[left + i].Clone());
            
            Track.Get(_multi).Window?.Selection.Select(_multi[right + 1], true);
        }

        public void Delete(int left, int right) {
            Region_Delete(left, right, out Action undo, out Action redo, out Action dispose);
            Program.Project.Undo.Add($"Chain Removed", undo, redo, dispose);
        }

        public void Group(int left, int right) {}
        public void Ungroup(int index) {}
        public void Choke(int left, int right) {}
        public void Unchoke(int index) {}
        
        public void Mute(int left, int right) {
            List<bool> u = (from i in Enumerable.Range(left, right - left + 1) select _multi[i].Enabled).ToList();
            bool r = !_multi[left].Enabled;

            List<int> path = Track.GetPath(_multi);

            Program.Project.Undo.Add($"Chain Muted", () => {
                Multi multi = Track.TraversePath<Multi>(path);

                for (int i = left; i <= right; i++)
                    multi[i].Enabled = u[i - left];

            }, () => {
                Multi multi = Track.TraversePath<Multi>(path);

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
        
        public async void Import(int right, string path = null) {
            Window sender = Track.Get(_multi).Window;
            
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