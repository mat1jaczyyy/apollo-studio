using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

using Apollo.Components;
using Apollo.Core;
using Apollo.Elements;

namespace Apollo.Viewers {
    public class ChainInfo: UserControl, ISelectViewer {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void ChainInfoEventHandler(int index);
        public event ChainInfoEventHandler ChainAdded;
        public event ChainInfoEventHandler ChainRemoved;

        public delegate void ChainExpandedEventHandler(int? index);
        public event ChainExpandedEventHandler ChainExpanded;

        Chain _chain;
        bool selected = false;

        Grid Root;
        TextBlock NameText;
        public VerticalAdd ChainAdd;

        Grid Draggable;
        ContextMenu ChainContextMenu;
        TextBox Input;

        private void UpdateText() => UpdateText(_chain.ParentIndex.Value, _chain.Name);
        private void UpdateText(int index) => UpdateText(index, _chain.Name);
        private void UpdateText(string name) => UpdateText(_chain.ParentIndex.Value, name);
        private void UpdateText(int index, string name) => NameText.Text = name.Replace("#", (index + 1).ToString());
        
        private void ApplyHeaderBrush(IBrush brush) {
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

        public ChainInfo(Chain chain) {
            InitializeComponent();
            
            _chain = chain;

            Root = this.Get<Grid>("DropZone");

            NameText = this.Get<TextBlock>("Name");
            UpdateText();
            _chain.ParentIndexChanged += UpdateText;
            _chain.NameChanged += UpdateText;

            ChainAdd = this.Get<VerticalAdd>("DropZoneAfter");

            ChainContextMenu = (ContextMenu)this.Resources["ChainContextMenu"];
            ChainContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(ContextMenu_Click));

            Draggable = this.Get<Grid>("Draggable");
            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);

            Input = this.Get<TextBox>("Input");
            Input.GetObservable(TextBox.TextProperty).Subscribe(Input_Changed);
        }

        private void Chain_Action(string action) => Track.Get(_chain).Window?.SelectionAction(action, (ISelectParent)_chain.Parent, _chain.ParentIndex.Value);

        private void ContextMenu_Click(object sender, EventArgs e) {
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Track.Get(_chain).Window?.SelectionAction((string)((MenuItem)item).Header);
        }

        private void Select(PointerPressedEventArgs e) {
            if (e.MouseButton == MouseButton.Left || (e.MouseButton == MouseButton.Right && !selected))
                Track.Get(_chain).Window?.Select(_chain, e.InputModifiers.HasFlag(InputModifiers.Shift));
        }

        public async void Drag(object sender, PointerPressedEventArgs e) {
            if (!selected) Select(e);

            DataObject dragData = new DataObject();
            dragData.Set("chain", Track.Get(_chain).Window?.Selection);

            DragDropEffects result = await DragDrop.DoDragDrop(dragData, DragDropEffects.Move);

            if (result == DragDropEffects.None) {
                if (selected) Select(e);
                
                if (e.MouseButton == MouseButton.Left)
                    ChainExpanded?.Invoke(_chain.ParentIndex.Value);
                
                if (e.MouseButton == MouseButton.Right)
                    ChainContextMenu.Open(Draggable);
            }
        }

        public void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.Contains("chain")) e.DragEffects = DragDropEffects.None; 
        }

        public void Drop(object sender, DragEventArgs e) {
            e.Handled = true;

            if (!e.Data.Contains("chain")) return;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZone" && source.Name != "DropZoneAfter")
                source = source.Parent;

            List<Chain> moving = ((List<ISelect>)e.Data.Get("chain")).Select(i => (Chain)i).ToList();
            bool copy = e.Modifiers.HasFlag(InputModifiers.Control);

            bool result;
            
            if (source.Name == "DropZone" && e.GetPosition(source).Y < source.Bounds.Height / 2) {
                if (_chain.ParentIndex == 0) result = Chain.Move(moving, (IMultipleChainParent)_chain.Parent, copy);
                else result = Chain.Move(moving, ((IMultipleChainParent)_chain.Parent)[_chain.ParentIndex.Value - 1], copy);
            } else result = Chain.Move(moving, _chain, copy);

            if (!result) e.DragEffects = DragDropEffects.None;
        }
        
        private void Chain_Add() => ChainAdded?.Invoke(_chain.ParentIndex.Value + 1);
        private void Chain_Remove() => ChainRemoved?.Invoke(_chain.ParentIndex.Value);

        private Action Input_Update;

        int Input_Left, Input_Right;

        private void Input_Changed(string text) {
            if (text == null) return;
            if (text == "") return;

            Input_Update = () => {
                for (int i = Input_Left; i <= Input_Right; i++)
                    ((IMultipleChainParent)_chain.Parent)[i].Name = text;
            };

            Dispatcher.UIThread.InvokeAsync(() => {
                Input_Update?.Invoke();
                Input_Update = null;
            });
        }

        public void StartInput(int left, int right) {
            Input_Left = left;
            Input_Right = right;

            Input.Text = _chain.Name;

            Input.Opacity = 1;
            Input.IsHitTestVisible = true;
            Input.Focus();
        }
        
        private void Input_LostFocus(object sender, RoutedEventArgs e) {
            Input.Text = _chain.Name;

            Input.Opacity = 0;
            Input.IsHitTestVisible = false;
        }

        private void Input_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Return)
                this.Focus();

            e.Handled = true;
        }

        private void Input_KeyUp(object sender, KeyEventArgs e) => e.Handled = true;

        private void Input_MouseUp(object sender, PointerReleasedEventArgs e) => e.Handled = true;
    }
}
