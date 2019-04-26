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
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.Viewers {
    public class DeviceViewer: UserControl {
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

        public delegate void DevicePastedEventHandler(int index, Device device);
        public event DevicePastedEventHandler DevicePasted;

        public delegate void DeviceRemovedEventHandler(int index);
        public event DeviceRemovedEventHandler DeviceRemoved;
        
        Device _device;
        bool selected = false;

        public StackPanel Root;
        public Border Border, Header;

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

        public void Device_Paste(Device device) => DevicePasted?.Invoke(_device.ParentIndex.Value + 1, device);

        public void Device_Remove() => DeviceRemoved?.Invoke(_device.ParentIndex.Value);

        private void ContextMenu_Click(object _, EventArgs e) {
            IInteractive sender = ((RoutedEventArgs)e).Source;

            if (sender.GetType() == typeof(MenuItem))
                Track.Get(_device).Window?.SelectionAction((string)((MenuItem)sender).Header);
        }

        public async void Drag(object sender, PointerPressedEventArgs e) {
            DataObject dragData = new DataObject();
            dragData.Set(Device.Identifier, _device);

            DragDropEffects result = await DragDrop.DoDragDrop(dragData, DragDropEffects.Move);

            if (result == DragDropEffects.None) {
                Track track = Track.Get(_device);

                if (e.MouseButton == MouseButton.Left || (e.MouseButton == MouseButton.Right && !selected))
                    track.Window?.Select(_device, e.InputModifiers.HasFlag(InputModifiers.Shift));
                
                if (e.MouseButton == MouseButton.Right) {
                    ContextMenu menu = DeviceContextMenu;
                    List<Device> selection = track.Window?.Selection;

                    if (selection.Count == 1 && selection[0].GetType() == typeof(Group) && ((Group)selection[0]).Count == 1)
                        menu = GroupContextMenu;

                    menu.Open(Draggable);
                }
            }
        }

        public void DragOver(object sender, DragEventArgs e) {
            if (!e.Data.Contains(Device.Identifier)) e.DragEffects = DragDropEffects.None; 
        }

        public void Drop(object sender, DragEventArgs e) {
            if (!e.Data.Contains(Device.Identifier)) return;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZoneHead" && source.Name != "Contents" && source.Name != "DropZoneTail" && source.Name != "DropZoneAfter")
                source = source.Parent;

            Device moving = (Device)e.Data.Get(Device.Identifier);
            bool copy = e.Modifiers.HasFlag(InputModifiers.Control);

            bool result;
            
            if (source.Name == "DropZoneHead" || (source.Name == "Contents" && e.GetPosition(source).X < source.Bounds.Width / 2)) {
                if (_device.ParentIndex == 0) result = moving.Move(_device.Parent, copy);
                else result = moving.Move(_device.Parent[_device.ParentIndex.Value - 1], copy);
            } else result = moving.Move(_device, copy);

            if (!result) e.DragEffects = DragDropEffects.None;

            e.Handled = true;
        }
    }
}
