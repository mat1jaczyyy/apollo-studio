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

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.Viewers {
    public class DeviceViewer: UserControl, ISelectViewer {
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

        public delegate void DeviceAddedEventHandler(int index, Type device);
        public event DeviceAddedEventHandler DeviceAdded;

        public delegate void DeviceRemovedEventHandler(int index);
        public event DeviceRemovedEventHandler DeviceRemoved;
        
        Device _device;
        bool selected = false;

        public StackPanel Root;
        public Border Border, Header;
        public DeviceAdd DeviceAdd;

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

            IControl _viewer = GetSpecificViewer(this, _device);

            if (_viewer != null)
                this.Get<Grid>("Contents").Children.Add(_viewer);
        }

        private void Device_Add(Type device) => DeviceAdded?.Invoke(_device.ParentIndex.Value + 1, device);

        private void Device_Remove() => DeviceRemoved?.Invoke(_device.ParentIndex.Value);

        private void Device_Action(string action) => Track.Get(_device).Window?.SelectionAction(action, _device.Parent, _device.ParentIndex.Value);

        private void ContextMenu_Click(object sender, EventArgs e) {
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Track.Get(_device).Window?.SelectionAction((string)((MenuItem)item).Header);
        }

        private void Select(PointerPressedEventArgs e) {
            if (e.MouseButton == MouseButton.Left || (e.MouseButton == MouseButton.Right && !selected))
                Track.Get(_device).Window?.Select(_device, e.InputModifiers.HasFlag(InputModifiers.Shift));
        }

        public async void Drag(object sender, PointerPressedEventArgs e) {
            if (!selected) Select(e);

            DataObject dragData = new DataObject();
            dragData.Set("device", Track.Get(_device).Window?.Selection);

            DragDropEffects result = await DragDrop.DoDragDrop(dragData, DragDropEffects.Move);

            if (result == DragDropEffects.None) {
                if (selected) Select(e);
                
                if (e.MouseButton == MouseButton.Right) {
                    ContextMenu menu = DeviceContextMenu;
                    List<ISelect> selection = Track.Get(_device).Window?.Selection;

                    if (selection.Count == 1 && selection[0].GetType() == typeof(Group) && ((Group)selection[0]).Count == 1)
                        menu = GroupContextMenu;

                    menu.Open(Draggable);
                }
            }
        }

        public void DragOver(object sender, DragEventArgs e) {
            if (!e.Data.Contains("device")) e.DragEffects = DragDropEffects.None; 
        }

        public void Drop(object sender, DragEventArgs e) {
            if (!e.Data.Contains("device")) return;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZoneHead" && source.Name != "Contents" && source.Name != "DropZoneTail" && source.Name != "DropZoneAfter")
                source = source.Parent;

            List<Device> moving = (List<Device>)e.Data.Get("device");
            bool copy = e.Modifiers.HasFlag(InputModifiers.Control);

            bool result;
            
            if (source.Name == "DropZoneHead" || (source.Name == "Contents" && e.GetPosition(source).X < source.Bounds.Width / 2)) {
                if (_device.ParentIndex == 0) result = Device.Move(moving, _device.Parent, copy);
                else result = Device.Move(moving, _device.Parent[_device.ParentIndex.Value - 1], copy);
            } else result = Device.Move(moving, _device, copy);

            if (!result) e.DragEffects = DragDropEffects.None;

            e.Handled = true;
        }
    }
}
