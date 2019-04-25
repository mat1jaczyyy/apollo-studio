using System;
using System.Linq;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

using Apollo.Components;
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

        public delegate void DeviceRemovedEventHandler(int index);
        public event DeviceRemovedEventHandler DeviceRemoved;
        
        Device _device;

        public StackPanel Root;
        public Border Border, Header;

        private void ApplyHeaderBrush(IBrush brush) {
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

        public void Select() => ApplyHeaderBrush((IBrush)Application.Current.Styles.FindResource("ThemeAccentBrush2"));
        public void Deselect() => ApplyHeaderBrush((IBrush)Application.Current.Styles.FindResource("ThemeControlLowBrush"));

        public DeviceViewer(Device device) {
            InitializeComponent();

            this.Get<TextBlock>("Title").Text = device.GetType().ToString().Split(".").Last();

            _device = device;
            _device.Viewer = this;

            Root = this.Get<StackPanel>("Root");

            Border = this.Get<Border>("Border");
            Header = this.Get<Border>("Header");
            Deselect();
            
            this.Get<Grid>("Draggable").PointerPressed += Drag;
            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);

            IControl _viewer = GetSpecificViewer(this, _device);

            if (_viewer != null)
                this.Get<Grid>("Contents").Children.Add(_viewer);
        }

        private void Device_Add(Type device) => DeviceAdded?.Invoke(_device.ParentIndex.Value + 1, device);

        private void Device_Remove() => DeviceRemoved?.Invoke(_device.ParentIndex.Value);

        public async void Drag(object sender, PointerPressedEventArgs e) {
            DataObject dragData = new DataObject();
            dragData.Set(Device.Identifier, _device);

            DragDropEffects result = await DragDrop.DoDragDrop(dragData, DragDropEffects.Move);

            if (result == DragDropEffects.None) _device.Parent.Viewer.Select(_device.ParentIndex);
        }

        public void DragOver(object sender, DragEventArgs e) {
            if (!e.Data.Contains(Device.Identifier)) e.DragEffects = DragDropEffects.None; 
        }

        public void Drop(object sender, DragEventArgs e) {
            if (!e.Data.Contains(Device.Identifier)) return;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZoneHead" && source.Name != "Contents" && source.Name != "DropZoneTail" && source.Name != "DropZoneAfter") source = source.Parent;

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
