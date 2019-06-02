using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

namespace Apollo.Viewers {
    public class DeviceViewer: UserControl, IDeviceViewer {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public static IControl GetSpecificViewer(DeviceViewer sender, Device device) {
            foreach (Type deviceViewer in (from type in Assembly.GetExecutingAssembly().GetTypes() where type.Namespace.StartsWith("Apollo.DeviceViewers") select type))      
                if ((string)deviceViewer.GetField("DeviceIdentifier").GetValue(null) == device.DeviceIdentifier) {
                    if (device.DeviceIdentifier == "group" || device.DeviceIdentifier == "multi")
                        return (IControl)Activator.CreateInstance(deviceViewer, new object[] {device, sender});
                    
                    return (IControl)Activator.CreateInstance(deviceViewer, new object[] {device});
                }

            return null;
        }

        public event DeviceAddedEventHandler DeviceAdded;
        public event DeviceCollapsedEventHandler DeviceCollapsed;
        
        Device _device;
        bool selected = false;

        public IControl SpecificViewer { get; private set; }

        public StackPanel Root;
        public Border Border, Header;
        public DeviceAdd DeviceAdd { get; private set; }

        Grid Draggable;
        ContextMenu DeviceContextMenu, GroupContextMenu;

        private void ApplyHeaderBrush(string resource) {
            IBrush brush = (IBrush)Application.Current.Styles.FindResource(resource);

            if (Root.Children[0].GetType() == typeof(DeviceHead)) {
                DeviceHead target = ((DeviceHead)Root.Children[0]);

                if (IsArrangeValid) target.Header.Background = brush;
                else target.Resources["TitleBrush"] = brush;
            }

            if (Root.Children[Root.Children.Count - 2].GetType() == typeof(DeviceTail)) {
                DeviceTail target = ((DeviceTail)Root.Children[Root.Children.Count - 2]);

                if (IsArrangeValid) target.Header.Background = brush;
                else target.Resources["TitleBrush"] = brush;
            }

            if (IsArrangeValid) Header.Background = brush;
            else this.Resources["TitleBrush"] = brush;
        }

        public void Select() {
            ApplyHeaderBrush("ThemeAccentBrush2");
            selected = true;
        }

        public void Deselect() {
            ApplyHeaderBrush("ThemeControlLowBrush");
            selected = false;
        }

        public DeviceViewer(Device device) {
            InitializeComponent();

            this.Get<TextBlock>("Title").Text = device.GetType().ToString().Split(".").Last();

            _device = device;
            _device.Viewer = this;

            Root = this.Get<StackPanel>("Root");

            Border = this.Get<Border>("Border");
            Header = this.Get<Border>("Header");
            Deselect();
            
            DeviceAdd = this.Get<DeviceAdd>("DropZoneAfter");

            DeviceContextMenu = (ContextMenu)this.Resources["DeviceContextMenu"];
            GroupContextMenu = (ContextMenu)this.Resources["GroupContextMenu"];
            
            DeviceContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(ContextMenu_Click));
            GroupContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(ContextMenu_Click));
            
            Draggable = this.Get<Grid>("Draggable");
            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);

            SpecificViewer = GetSpecificViewer(this, _device);

            if (SpecificViewer != null)
                this.Get<Grid>("Contents").Children.Add(SpecificViewer);
        }

        private void Device_Add(Type device) => DeviceAdded?.Invoke(_device.ParentIndex.Value + 1, device);

        private void Device_Action(string action) => Track.Get(_device)?.Window?.Selection.Action(action, _device.Parent, _device.ParentIndex.Value);

        private void ContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Track.Get(_device)?.Window?.Selection.Action((string)((MenuItem)item).Header);
        }

        private void Select(PointerPressedEventArgs e) {
            if (e.MouseButton == MouseButton.Left || (e.MouseButton == MouseButton.Right && !selected))
                Track.Get(_device)?.Window?.Selection.Select(_device, e.InputModifiers.HasFlag(InputModifiers.Shift));
        }

        public async void Drag(object sender, PointerPressedEventArgs e) {
            if (!selected) Select(e);

            DataObject dragData = new DataObject();
            dragData.Set("device", Track.Get(_device)?.Window?.Selection.Selection);

            DragDropEffects result = await DragDrop.DoDragDrop(dragData, DragDropEffects.Move);

            if (result == DragDropEffects.None) {
                if (selected) Select(e);
                
                if (e.MouseButton == MouseButton.Right) {
                    ContextMenu menu = DeviceContextMenu;
                    List<ISelect> selection = Track.Get(_device)?.Window?.Selection.Selection;

                    if (selection.Count == 1 && selection[0].GetType() == typeof(Group) && ((Group)selection[0]).Count == 1)
                        menu = GroupContextMenu;

                    menu.Open(Draggable);
                
                } else if (e.MouseButton == MouseButton.Left && e.ClickCount == 2) {
                    _device.Collapsed = !_device.Collapsed;
                    DeviceCollapsed?.Invoke(_device.ParentIndex.Value);
                }
            }
        }

        public void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.Contains("device")) e.DragEffects = DragDropEffects.None; 
        }

        public void Drop(object sender, DragEventArgs e) {
            e.Handled = true;

            if (!e.Data.Contains("device")) return;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZoneHead" && source.Name != "Contents" && source.Name != "DropZoneTail" && source.Name != "DropZoneAfter") {
                source = source.Parent;
                
                if (source == this) {
                    e.Handled = false;
                    return;
                }
            }

            List<Device> moving = ((List<ISelect>)e.Data.Get("device")).Select(i => (Device)i).ToList();

            Chain source_parent = moving[0].Parent;
            Chain _chain = _device.Parent;

            int before = moving[0].IParentIndex.Value - 1;
            int after = _device.ParentIndex.Value;
            if (source.Name == "DropZoneHead" || (source.Name == "Contents" && e.GetPosition(source).X < source.Bounds.Width / 2)) after--;

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
    }
}
