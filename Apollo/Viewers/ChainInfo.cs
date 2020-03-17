using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.DragDrop;
using Apollo.Elements;
using Apollo.Selection;
using Apollo.Windows;

namespace Apollo.Viewers {
    public class ChainInfo: UserControl, ISelectViewer, IDraggable {
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
        public bool Selected { get; private set; } = false;

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
            Selected = true;
        }

        public void Deselect() {
            ApplyHeaderBrush(new SolidColorBrush(Color.Parse("Transparent")));
            Selected = false;
        }

        public ChainInfo() => new InvalidOperationException();

        public ChainInfo(Chain chain) {
            InitializeComponent();
            
            _chain = chain;

            Deselect();

            UpdateText();
            _chain.ParentIndexChanged += UpdateText;

            DragDrop = new DragDropManager(this);

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

            DragDrop.Dispose();
            DragDrop = null;
        }

        public void SetEnabled() => NameText.Foreground = (IBrush)Application.Current.Styles.FindResource(_chain.Enabled? "ThemeForegroundBrush" : "ThemeForegroundLowBrush");

        void Chain_Action(string action) => Track.Get(_chain)?.Window?.Selection.Action(action, (ISelectParent)_chain.Parent, _chain.ParentIndex.Value);

        void ContextMenu_Action(string action) => Track.Get(_chain)?.Window?.Selection.Action(action);

        public void Select(PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed || (MouseButton == PointerUpdateKind.RightButtonPressed && !Selected))
                Track.Get(_chain)?.Window?.Selection.Select(_chain, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
        }

        public static bool DeviceAsChainDrop(IControl source, ISelectParent parent, ISelect child, int after, string format, DragEventArgs e) {
            List<Device> moving = ((List<ISelect>)e.Data.Get(format)).Cast<Device>().ToList();

            Chain source_chain = moving[0].Parent;
            Chain target_chain = (Chain)child;

            int before = moving[0].IParentIndex.Value - 1;
            bool copy = e.KeyModifiers.HasFlag(App.ControlKey);

            int? remove = null;

            Group parent_group = (Group)parent;

            if (target_chain == null) {
                parent_group.Insert((remove = (source.Name != "DropZoneAfter")? 0 : parent_group.Count).Value, target_chain = new Chain());

            } else {
                if (source.Name != "DropZoneAfter") {
                    if (parent_group.Expanded != target_chain.IParentIndex)
                        parent_group.SpecificViewer.Expand(target_chain.IParentIndex);
                
                } else {
                    parent_group.Insert((remove = target_chain.IParentIndex + 1).Value);
                    target_chain = parent_group[target_chain.IParentIndex.Value + 1];
                }
            }

            bool result;

            if (result = DragDropManager.Move(moving.Cast<ISelect>().ToList(), (ISelectParent)target_chain, after = ((ISelectParent)target_chain).Count - 1, copy)) {
                int before_pos = before;
                int after_pos = moving[0].IParentIndex.Value - 1;
                int count = moving.Count;

                if (source_chain == target_chain && after < before)
                    before_pos += count;
                
                List<int> sourcepath = Track.GetPath(source_chain);
                List<int> targetpath = Track.GetPath(target_chain);
                
                Program.Project.Undo.Add($"{format} {(copy? "Copied" : "Moved")}", copy
                    ? new Action(() => {
                        Chain targetchain = Track.TraversePath<Chain>(targetpath);

                        for (int i = after + count; i > after; i--)
                            targetchain.Remove(i);
                        
                        if (remove != null)
                            ((Group)targetchain.Parent).Remove(remove.Value);

                    }) : new Action(() => {
                        Chain sourcechain = Track.TraversePath<Chain>(sourcepath);
                        Chain targetchain = Track.TraversePath<Chain>(targetpath);

                        List<Device> umoving = (from i in Enumerable.Range(after_pos + 1, count) select targetchain[i]).ToList();

                        DragDropManager.Move(umoving.Cast<ISelect>().ToList(), (ISelectParent)sourcechain, before_pos);

                        if (remove != null)
                            ((Group)targetchain.Parent).Remove(remove.Value);

                }), () => {
                    Chain sourcechain = Track.TraversePath<Chain>(sourcepath);
                    Chain targetchain;

                    if (remove != null) Track.TraversePath<Group>(targetpath.Skip(1).ToList()).Insert(remove.Value, targetchain = new Chain());
                    else targetchain = Track.TraversePath<Chain>(targetpath);

                    List<Device> rmoving = (from i in Enumerable.Range(before + 1, count) select sourcechain[i]).ToList();

                    DragDropManager.Move(rmoving.Cast<ISelect>().ToList(), (ISelectParent)targetchain, after, copy);
                });

            } else if (remove != null) parent_group.Remove(remove.Value);

            return result;
        }

        DragDropManager DragDrop;

        public string DragFormat => "Chain";
        public List<string> DropAreas => new List<string>() {"DropZone", "DropZoneAfter"};

        public Dictionary<string, DragDropManager.DropHandler> DropHandlers => new Dictionary<string, DragDropManager.DropHandler>() {
            {DataFormats.FileNames, null},
            {DragFormat, null},
            {"Device", DeviceAsChainDrop}
        };

        public ISelect Item => _chain;
        public ISelectParent ItemParent => Item.IParent;

        public void DragFailed(PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;
                
            if (MouseButton == PointerUpdateKind.LeftButtonPressed)
                ChainExpanded?.Invoke(_chain.ParentIndex.Value);
            
            if (MouseButton == PointerUpdateKind.RightButtonPressed) {
                MuteItem.Header = ((Chain)Track.Get(_chain)?.Window?.Selection.Selection.First()).Enabled? "Mute" : "Unmute";
                ((ApolloContextMenu)this.Resources["ChainContextMenu"]).Open(Draggable);
            }
        }

        public void Drag(object sender, PointerPressedEventArgs e) => DragDrop.Drag(Track.Get(_chain)?.Window?.Selection, e);
        
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
                ((Group)_chain.Parent)[i].Name = text;
            Input_Ignore = false;
        }

        public void StartInput(int left, int right) {
            Input_Left = left;
            Input_Right = right;

            Input_Clean = new List<string>();
            for (int i = left; i <= right; i++)
                Input_Clean.Add(((Group)_chain.Parent)[i].Name);

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
            
            List<string> newName = (from i in Enumerable.Range(0, Input_Clean.Count) select Input.Text).ToList();

            if (!newName.SequenceEqual(Input_Clean))
                Program.Project.Undo.Add(new Chain.RenamedUndoEntry(
                    (Group)_chain.Parent,
                    Input_Left,
                    Input_Right,
                    Input_Clean,
                    newName
                ));
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
