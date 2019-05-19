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

namespace Apollo.Viewers {
    public class ChainViewer: UserControl, ISelectParentViewer {
        public int? IExpanded {
            get => null;
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Chain _chain;

        Grid DropZoneBefore, DropZoneAfter;
        ContextMenu DeviceContextMenuBefore, DeviceContextMenuAfter;

        Controls Contents;
        DeviceAdd DeviceAdd;

        private void SetAlwaysShowing() {
            bool RootChain = _chain.Parent.GetType() == typeof(Track);

            DeviceAdd.AlwaysShowing = Contents.Count == 1;

            for (int i = 1; i < Contents.Count; i++)
                ((DeviceViewer)Contents[i]).DeviceAdd.AlwaysShowing = false;

            if (Contents.Count > 1 && RootChain) ((DeviceViewer)Contents.Last()).DeviceAdd.AlwaysShowing = true;
        }

        public void Contents_Insert(int index, Device device) {
            DeviceViewer viewer = new DeviceViewer(device);
            viewer.DeviceAdded += Device_Insert;
            viewer.DeviceRemoved += Device_Remove;

            Contents.Insert(index + 1, viewer);
            SetAlwaysShowing();
        }

        public void Contents_Remove(int index) {
            Contents.RemoveAt(index + 1);
            SetAlwaysShowing();
        }

        public ChainViewer(Chain chain, bool backgroundBorder = false) {
            InitializeComponent();

            _chain = chain;
            _chain.Viewer = this;

            DropZoneBefore = this.Get<Grid>("DropZoneBefore");
            DropZoneAfter = this.Get<Grid>("DropZoneAfter");
            
            DeviceContextMenuBefore = (ContextMenu)this.Resources["DeviceContextMenuBefore"];
            DeviceContextMenuBefore.AddHandler(MenuItem.ClickEvent, new EventHandler(DeviceContextMenu_Click));
            
            DeviceContextMenuAfter = (ContextMenu)this.Resources["DeviceContextMenuAfter"];
            DeviceContextMenuAfter.AddHandler(MenuItem.ClickEvent, new EventHandler(DeviceContextMenu_Click));

            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);

            Contents = this.Get<StackPanel>("Contents").Children;
            DeviceAdd = this.Get<DeviceAdd>("DeviceAdd");

            for (int i = 0; i < _chain.Count; i++)
                Contents_Insert(i, _chain[i]);
            
            if (backgroundBorder) {
                this.Get<Grid>("Root").Children.Insert(0, new DeviceBackground());
                Background = (IBrush)Application.Current.Styles.FindResource("ThemeControlDarkenBrush");
            }
        }

        public void Expand(int? index) {}

        private void Device_Insert(int index, Type device) => Device_Insert(index, Device.Create(device, _chain));
        private void Device_InsertStart(Type device) => Device_Insert(0, device);

        private void Device_Insert(int index, Device device) {
            Device r = device.Clone();
            List<int> path = Track.GetPath(_chain);

            Program.Project.Undo.Add($"Device Added", () => {
                ((Chain)Track.TraversePath(path)).Remove(index);
            }, () => {
                ((Chain)Track.TraversePath(path)).Insert(index, r.Clone());
            });
            
            _chain.Insert(index, device);
        }

        private void Device_Remove(int index) {
            Device u = _chain[index].Clone();
            List<int> path = Track.GetPath(_chain);

            Program.Project.Undo.Add($"Device Deleted", () => {
                ((Chain)Track.TraversePath(path)).Insert(index, u.Clone());
            }, () => {
                ((Chain)Track.TraversePath(path)).Remove(index);
            });

            _chain.Remove(index);
        }

        private void Device_Action(string action) => Device_Action(action, false);
        private void Device_Action(string action, bool right) => Track.Get(_chain).Window?.Selection.Action(action, _chain, (right? _chain.Count : 0) - 1);

        private void DeviceContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Device_Action((string)((MenuItem)item).Header, sender == DeviceContextMenuAfter);
        }

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Right) 
                if (sender == DropZoneBefore) DeviceContextMenuBefore.Open((Control)sender);
                else if (sender == DropZoneAfter) DeviceContextMenuAfter.Open((Control)sender);

            e.Handled = true;
        }

        private void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.Contains("device")) e.DragEffects = DragDropEffects.None; 
        }

        private void Drop(object sender, DragEventArgs e) {
            e.Handled = true;

            if (!e.Data.Contains("device")) return;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZoneBefore" && source.Name != "DropZoneAfter" && source.Name != "DeviceAdd")
                source = source.Parent;

            List<Device> moving = ((List<ISelect>)e.Data.Get("device")).Select(i => (Device)i).ToList();

            Chain source_parent = moving[0].Parent;

            int before = moving[0].IParentIndex.Value - 1;
            int after = (source.Name == "DropZoneAfter")? _chain.Count - 1 : -1;

            bool copy = e.Modifiers.HasFlag(InputModifiers.Control);

            bool result = Device.Move(moving, _chain, after, copy);
            
            if (result) {
                int before_pos = before;
                int after_pos = moving[0].IParentIndex.Value - 1;
                int count = moving.Count;

                if (source_parent == _chain && after < before)
                    before_pos += count;
                
                List<int> sourcepath = Track.GetPath(source_parent);
                List<int> targetpath = Track.GetPath(_chain);
                
                Program.Project.Undo.Add(copy? $"Device Copied" : $"Device Moved", copy
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
            
            Copyable paste = Decoder.Decode(new MemoryStream(Convert.FromBase64String(b64)), typeof(Copyable));
            
            for (int i = 0; i < paste.Contents.Count; i++)
                Device_Insert(right + i + 1, (Device)paste.Contents[i]);
        }

        public void Duplicate(int left, int right) {
            for (int i = 0; i <= right - left; i++)
                Device_Insert(right + i + 1, _chain.Devices[left + i].Clone());
        }

        public void Delete(int left, int right) {
            for (int i = right; i >= left; i--)
                Device_Remove(i);
        }

        public void Group(int left, int right) {
            Chain init = new Chain();

            for (int i = left; i <= right; i++)
                init.Add(_chain.Devices[i]);
            
            for (int i = right; i >= left; i--) {
                Contents_Remove(i);
                _chain.Remove(i, false);
            }

            Device_Insert(left, new Group(new List<Chain>() {init}));
        }

        public void Ungroup(int index) {
            List<Device> init = ((Group)_chain.Devices[index])[0].Devices;

            Contents_Remove(index);
            _chain.Remove(index, false);
            
            for (int i = 0; i < init.Count; i++)
                Device_Insert(index + i, init[i]);
        }

        public void Rename(int left, int right) => throw new InvalidOperationException("A Device cannot be renamed.");
    }
}
