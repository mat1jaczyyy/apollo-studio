using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Interfaces;
using Apollo.Windows;

namespace Apollo.Viewers {
    public class ChainInfo: UserControl, ISelectViewer {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Root = this.Get<Grid>("DropZone");
            ChainAdd = this.Get<VerticalAdd>("DropZoneAfter");
            NameText = this.Get<TextBlock>("Name");
            Draggable = this.Get<Grid>("Draggable");
            MuteItem = this.Get<MenuItem>("MuteItem");
            Input = this.Get<TextBox>("Input");
            Indicator = this.Get<Indicator>("Indicator");
        }
        
        IDisposable observable;

        public delegate void ChainAddedEventHandler(int index);
        public event ChainAddedEventHandler ChainAdded;

        public delegate void ChainExpandedEventHandler(int? index);
        public event ChainExpandedEventHandler ChainExpanded;

        Chain _chain;
        bool selected = false;

        Grid Root;
        TextBlock NameText;
        public VerticalAdd ChainAdd;
        public Indicator Indicator { get; private set; }

        Grid Draggable;
        MenuItem MuteItem;
        TextBox Input;

        void UpdateText() => NameText.Text = _chain.ProcessedName;
        
        void ApplyHeaderBrush(IBrush brush) {
            if (IsArrangeValid) Root.Background = brush;
            else this.Resources["BackgroundBrush"] = brush;
        }

        public void Select() {
            ApplyHeaderBrush((IBrush)Application.Current.Styles.FindResource("ThemeAccentBrush2"));
            selected = true;
        }

        public void Deselect() {
            ApplyHeaderBrush(new SolidColorBrush(Color.Parse("Transparent")));
            selected = false;
        }

        public ChainInfo() => new InvalidOperationException();

        public ChainInfo(Chain chain) {
            InitializeComponent();
            
            _chain = chain;

            Deselect();

            UpdateText();
            _chain.ParentIndexChanged += UpdateText;

            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);

            observable = Input.GetObservable(TextBox.TextProperty).Subscribe(Input_Changed);

            SetEnabled();
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            ChainAdded = null;
            ChainExpanded = null;

            _chain.ParentIndexChanged -= UpdateText;
            _chain.Info = null;
            _chain = null;

            observable.Dispose();

            this.RemoveHandler(DragDrop.DropEvent, Drop);
            this.RemoveHandler(DragDrop.DragOverEvent, DragOver);
        }

        public void SetEnabled() => NameText.Foreground = (IBrush)Application.Current.Styles.FindResource(_chain.Enabled? "ThemeForegroundBrush" : "ThemeForegroundLowBrush");

        void Chain_Action(string action) => Track.Get(_chain)?.Window?.Selection.Action(action, (ISelectParent)_chain.Parent, _chain.ParentIndex.Value);

        void ContextMenu_Action(string action) => Track.Get(_chain)?.Window?.Selection.Action(action);

        void Select(PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed || (MouseButton == PointerUpdateKind.RightButtonPressed && !selected))
                Track.Get(_chain)?.Window?.Selection.Select(_chain, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
        }

        public async void Drag(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (!selected) Select(e);

            DataObject dragData = new DataObject();
            List<Chain> chains = Track.Get(_chain)?.Window?.Selection.Selection.OfType<Chain>().ToList();
            dragData.Set("chain", chains);

            if (_chain.Parent is Multi multi)
                dragData.Set("filters", chains.Select(i => multi.GetFilter(multi.Chains.IndexOf(i)).ToArray()).ToList());

            App.Dragging = true;
            DragDropEffects result = await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
            App.Dragging = false;

            if (result == DragDropEffects.None) {
                if (selected) Select(e);
                
                if (MouseButton == PointerUpdateKind.LeftButtonPressed)
                    ChainExpanded?.Invoke(_chain.ParentIndex.Value);
                
                if (MouseButton == PointerUpdateKind.RightButtonPressed) {
                    MuteItem.Header = ((Chain)Track.Get(_chain)?.Window?.Selection.Selection.First()).Enabled? "Mute" : "Unmute";
                    ((ApolloContextMenu)this.Resources["ChainContextMenu"]).Open(Draggable);
                }
            }
        }

        public void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.Contains("chain") && !e.Data.Contains("device") && !e.Data.Contains(DataFormats.FileNames)) e.DragEffects = DragDropEffects.None; 
        }

        public void Drop(object sender, DragEventArgs e) {
            e.Handled = true;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZone" && source.Name != "DropZoneAfter") {
                source = source.Parent;
                
                if (source == this) {
                    e.Handled = false;
                    return;
                }
            }

            IMultipleChainParent _device = (IMultipleChainParent)_chain.Parent;
            
            int after = _chain.ParentIndex.Value;
            if (source.Name == "DropZone" && e.GetPosition(source).Y < source.Bounds.Height / 2) after--;

            if (e.Data.Contains(DataFormats.FileNames)) {
                string path = e.Data.GetFileNames().FirstOrDefault();

                if (path != null) _device.SpecificViewer?.Import(after, path);

                return;
            }

            bool copy = e.Modifiers.HasFlag(App.ControlInput);
            bool result;

            if (e.Data.Contains("chain")) {
                List<Chain> moving = (List<Chain>)e.Data.Get("chain");

                List<bool[]> movingfilters = e.Data.Contains("filters")
                    ? (List<bool[]>)e.Data.Get("filters")
                    : null;

                IMultipleChainParent source_parent = (IMultipleChainParent)moving[0].Parent;

                int before = moving[0].IParentIndex.Value - 1;

                if (result = Chain.Move(moving, _device, after, copy, movingfilters)) {
                    int before_pos = before;
                    int after_pos = moving[0].IParentIndex.Value - 1;
                    int count = moving.Count;

                    if (source_parent == _device && after < before)
                        before_pos += count;
                    
                    List<int> sourcepath = Track.GetPath((ISelect)source_parent);
                    List<int> targetpath = Track.GetPath((ISelect)_device);
                    
                    Program.Project.Undo.Add($"Chain {(copy? "Copied" : "Moved")}", copy
                        ? new Action(() => {
                            IMultipleChainParent targetdevice = Track.TraversePath<IMultipleChainParent>(targetpath);

                            for (int i = after + count; i > after; i--)
                                targetdevice.Remove(i);

                        }) : new Action(() => {
                            IMultipleChainParent sourcedevice = Track.TraversePath<IMultipleChainParent>(sourcepath);
                            IMultipleChainParent targetdevice = Track.TraversePath<IMultipleChainParent>(targetpath);

                            List<Chain> umoving = (from i in Enumerable.Range(after_pos + 1, count) select targetdevice[i]).ToList();

                            Chain.Move(umoving, sourcedevice, before_pos, multiFilters: movingfilters);

                    }), () => {
                        IMultipleChainParent sourcedevice = Track.TraversePath<IMultipleChainParent>(sourcepath);
                        IMultipleChainParent targetdevice = Track.TraversePath<IMultipleChainParent>(targetpath);

                        List<Chain> rmoving = (from i in Enumerable.Range(before + 1, count) select sourcedevice[i]).ToList();

                        Chain.Move(rmoving, targetdevice, after, copy, movingfilters);
                    });
                }

            } else if (e.Data.Contains("device")) {
                List<Device> moving = ((List<ISelect>)e.Data.Get("device")).Select(i => (Device)i).ToList();

                Chain source_chain = moving[0].Parent;
                Chain target_chain = _chain;

                int before = moving[0].IParentIndex.Value - 1;

                int? remove = null;

                if (source.Name == "DropZone") {
                    if (((IMultipleChainParent)_chain.Parent).Expanded != _chain.ParentIndex)
                        ((IMultipleChainParent)_chain.Parent).SpecificViewer.Expand(_chain.ParentIndex);
                
                } else {
                    ((IMultipleChainParent)_chain.Parent).Insert((remove = _chain.ParentIndex + 1).Value);
                    target_chain = ((IMultipleChainParent)_chain.Parent)[_chain.ParentIndex.Value + 1];
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
                            
                            if (remove != null)
                                ((IMultipleChainParent)targetchain.Parent).Remove(remove.Value);

                        }) : new Action(() => {
                            Chain sourcechain = Track.TraversePath<Chain>(sourcepath);
                            Chain targetchain = Track.TraversePath<Chain>(targetpath);

                            List<Device> umoving = (from i in Enumerable.Range(after_pos + 1, count) select targetchain[i]).ToList();

                            Device.Move(umoving, sourcechain, before_pos);

                            if (remove != null)
                                ((IMultipleChainParent)targetchain.Parent).Remove(remove.Value);

                    }), () => {
                        Chain sourcechain = Track.TraversePath<Chain>(sourcepath);
                        Chain targetchain;

                        if (remove != null) {
                            IMultipleChainParent target = Track.TraversePath<IMultipleChainParent>(targetpath.Skip(1).ToList());
                            target.Insert(remove.Value);
                            targetchain = target[remove.Value];
                        
                        } else targetchain = Track.TraversePath<Chain>(targetpath);

                        List<Device> rmoving = (from i in Enumerable.Range(before + 1, count) select sourcechain[i]).ToList();

                        Device.Move(rmoving, targetchain, after, copy);
                    });

                } else if (remove != null) ((IMultipleChainParent)_chain.Parent).Remove(remove.Value);

            } else return;

            if (!result) e.DragEffects = DragDropEffects.None;
        }
        
        void Chain_Add() => ChainAdded?.Invoke(_chain.ParentIndex.Value + 1);

        int Input_Left, Input_Right;
        List<string> Input_Clean;
        bool Input_Ignore = false;

        void Input_Changed(string text) {
            if (text == null) return;
            if (text == "") return;

            if (Input_Ignore) return;

            Input_Ignore = true;
            for (int i = Input_Left; i <= Input_Right; i++)
                ((IMultipleChainParent)_chain.Parent)[i].Name = text;
            Input_Ignore = false;
        }

        public void StartInput(int left, int right) {
            Input_Left = left;
            Input_Right = right;

            Input_Clean = new List<string>();
            for (int i = left; i <= right; i++)
                Input_Clean.Add(((IMultipleChainParent)_chain.Parent)[i].Name);

            Input.Text = _chain.Name;
            Input.SelectionStart = 0;
            Input.SelectionEnd = Input.Text.Length;
            Input.CaretIndex = Input.Text.Length;

            Input.Opacity = 1;
            Input.IsHitTestVisible = true;
            Input.Focus();
        }

        void Input_LostFocus(object sender, RoutedEventArgs e) {
            Input.Text = _chain.Name;

            Input.Opacity = 0;
            Input.IsHitTestVisible = false;

            List<string> r = (from i in Enumerable.Range(0, Input_Clean.Count) select Input.Text).ToList();

            if (!r.SequenceEqual(Input_Clean)) {
                int left = Input_Left;
                int right = Input_Right;
                List<string> u = (from i in Input_Clean select i).ToList();
                List<int> path = Track.GetPath(_chain);

                Program.Project.Undo.Add($"Chain Renamed to {Input.Text}", () => {
                    Chain chain = Track.TraversePath<Chain>(path);
                    IMultipleChainParent parent = (IMultipleChainParent)chain.Parent;

                    for (int i = left; i <= right; i++)
                        parent[i].Name = u[i - left];
                    
                    TrackWindow window = Track.Get(chain)?.Window;

                    window?.Selection.Select(parent[left]);
                    window?.Selection.Select(parent[right], true);
                    
                }, () => {
                    Chain chain = Track.TraversePath<Chain>(path);
                    IMultipleChainParent parent = (IMultipleChainParent)chain.Parent;

                    for (int i = left; i <= right; i++)
                        parent[i].Name = r[i - left];
                    
                    TrackWindow window = Track.Get(chain)?.Window;

                    window?.Selection.Select(parent[left]);
                    window?.Selection.Select(parent[right], true);
                });
            }
        }

        public void SetName(string name) {
            UpdateText();

            if (Input_Ignore) return;

            Input_Ignore = true;
            Input.Text = name;
            Input_Ignore = false;
        }

        void Input_KeyDown(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            if (e.Key == Key.Return)
                this.Focus();

            e.Key = Key.None;
        }

        void Input_KeyUp(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            e.Key = Key.None;
        }

        void Input_MouseUp(object sender, PointerReleasedEventArgs e) => e.Handled = true;
    }
}
