using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaColor = Avalonia.Media.Color;
using IBrush = Avalonia.Media.IBrush;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;
using Avalonia.VisualTree;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Interfaces;
using Apollo.Structures;

namespace Apollo.Components {
    public class FrameDisplay: UserControl, ISelectViewer {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Root = this.Get<StackPanel>("DropZone");

            Viewer = this.Get<FrameThumbnail>("Draggable");
            
            Remove = this.Get<Remove>("Remove");
            FrameAdd = this.Get<VerticalAdd>("DropZoneAfter");
        }

        public delegate void FrameEventHandler(int index);
        public event FrameEventHandler FrameAdded;
        public event FrameEventHandler FrameRemoved;
        public event FrameEventHandler FrameSelected;
        
        Pattern _pattern;
        bool selected = false;
        public FrameThumbnail Viewer;

        StackPanel Root;
        public Remove Remove;
        public VerticalAdd FrameAdd;
        ContextMenu FrameContextMenu;

        void ApplyHeaderBrush(IBrush brush) {
            if (IsArrangeValid) Root.Background = brush;
            else this.Resources["BackgroundBrush"] = brush;
        }

        public void Select() {
            ApplyHeaderBrush((IBrush)Application.Current.Styles.FindResource("ThemeAccentBrush2"));
            selected = true;
        }

        public void Deselect() {
            ApplyHeaderBrush(new SolidColorBrush(AvaloniaColor.Parse("Transparent")));
            selected = false;
        }

        public FrameDisplay(Frame frame, Pattern pattern) {
            InitializeComponent();

            _pattern = pattern;

            Viewer.Frame = frame;

            FrameContextMenu = (ContextMenu)this.Resources["FrameContextMenu"];
            FrameContextMenu.AddHandler(MenuItem.ClickEvent, ContextMenu_Click);

            this.AddHandler(DragDrop.DragOverEvent, DragOver);
            this.AddHandler(DragDrop.DropEvent, Drop);
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            FrameAdded = null;
            FrameRemoved = null;
            FrameSelected = null;
            Viewer = null;

            this.RemoveHandler(DragDrop.DragOverEvent, DragOver);
            this.RemoveHandler(DragDrop.DropEvent, Drop);

            FrameContextMenu.RemoveHandler(MenuItem.ClickEvent, ContextMenu_Click);
            FrameContextMenu = null;
        }

        void Frame_Action(string action) => _pattern.Window?.Selection.Action(action, _pattern, Viewer.Frame.ParentIndex.Value);

        void ContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                _pattern.Window?.Selection.Action((string)((MenuItem)item).Header);
        }

        void Select(PointerPressedEventArgs e) {
            if (e.MouseButton == MouseButton.Left || (e.MouseButton == MouseButton.Right && !selected))
                _pattern.Window?.Selection.Select(Viewer.Frame, e.InputModifiers.HasFlag(InputModifiers.Shift));
        }

        public async void Drag(object sender, PointerPressedEventArgs e) {
            if (!selected) Select(e);

            DataObject dragData = new DataObject();
            dragData.Set("frame", _pattern.Window?.Selection.Selection);

            DragDropEffects result = await DragDrop.DoDragDrop(dragData, DragDropEffects.Move);

            if (result == DragDropEffects.None) {
                if (selected) Select(e);
                
                if (e.MouseButton == MouseButton.Left)
                    FrameSelected?.Invoke(Viewer.Frame.ParentIndex.Value);
        
                if (e.MouseButton == MouseButton.Right)
                    FrameContextMenu.Open(Viewer);
            }
        }

        public void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.Contains("frame")) e.DragEffects = DragDropEffects.None; 
        }

        public void Drop(object sender, DragEventArgs e) {
            e.Handled = true;

            if (!e.Data.Contains("frame")) return;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZone" && source.Name != "DropZoneAfter") {
                source = source.Parent;
                
                if (source == this) {
                    e.Handled = false;
                    return;
                }
            }

            List<Frame> moving = ((List<ISelect>)e.Data.Get("frame")).Select(i => (Frame)i).ToList();

            Pattern source_parent = moving[0].Parent;

            int before = moving[0].IParentIndex.Value - 1;
            int after = Viewer.Frame.ParentIndex.Value;
            if (source.Name == "DropZone" && e.GetPosition(source).Y < source.Bounds.Height / 2) after--;

            bool copy = e.Modifiers.HasFlag(InputModifiers.Control);
            
            bool result = Frame.Move(moving, _pattern, after, copy);

            if (result) {
                int before_pos = before;
                int after_pos = moving[0].IParentIndex.Value - 1;
                int count = moving.Count;

                if (source_parent == _pattern && after < before)
                    before_pos += count;
                
                List<int> sourcepath = Track.GetPath(source_parent);
                List<int> targetpath = Track.GetPath(_pattern);
                
                Program.Project.Undo.Add(copy? $"Pattern Frame Copied" : $"Pattern Frame Moved", copy
                    ? new Action(() => {
                        Pattern targetpattern = ((Pattern)Track.TraversePath(targetpath));

                        for (int i = after + count; i > after; i--)
                            targetpattern.Remove(i);

                    }) : new Action(() => {
                        Pattern sourcepattern = ((Pattern)Track.TraversePath(sourcepath));
                        Pattern targetpattern = ((Pattern)Track.TraversePath(targetpath));

                        List<Frame> umoving = (from i in Enumerable.Range(after_pos + 1, count) select targetpattern[i]).ToList();

                        Frame.Move(umoving, sourcepattern, before_pos);

                }), () => {
                    Pattern sourcepattern = ((Pattern)Track.TraversePath(sourcepath));
                    Pattern targetpattern = ((Pattern)Track.TraversePath(targetpath));

                    List<Frame> rmoving = (from i in Enumerable.Range(before + 1, count) select sourcepattern[i]).ToList();

                    Frame.Move(rmoving, targetpattern, after, copy);
                });
            
            } else e.DragEffects = DragDropEffects.None;
        }
        
        void Frame_Add() => FrameAdded?.Invoke(Viewer.Frame.ParentIndex.Value + 1);
        void Frame_Remove() => FrameRemoved?.Invoke(Viewer.Frame.ParentIndex.Value);
    }
}
