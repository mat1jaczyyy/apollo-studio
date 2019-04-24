using System;
using System.Linq;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

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

        public DeviceViewer(Device device) {
            InitializeComponent();

            this.Get<TextBlock>("Title").Text = device.GetType().ToString().Split(".").Last();

            _device = device;
            _device.Viewer = this;
            
            this.Get<Grid>("Draggable").PointerPressed += Drag;
            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);
            
            IControl _viewer = GetSpecificViewer(this, _device);
            ((UserControl)_viewer).Margin = new Thickness(10, 5);

            if (_viewer != null)
                this.Get<Grid>("Contents").Children.Add(_viewer);
        }

        private void Device_Add(Type device) => DeviceAdded?.Invoke(_device.ParentIndex.Value + 1, device);

        private void Device_Remove() => DeviceRemoved?.Invoke(_device.ParentIndex.Value);

        private async void Drag(object sender, PointerPressedEventArgs e) {
            DataObject dragData = new DataObject();
            dragData.Set(Device.Identifier, _device);
            await DragDrop.DoDragDrop(dragData, DragDropEffects.Move);
        }

        private void DragOver(object sender, DragEventArgs e) {
            if (!e.Data.Contains(Device.Identifier)) e.DragEffects = DragDropEffects.None; 
        }

        private void Drop(object sender, DragEventArgs e) {
            if (!e.Data.Contains(Device.Identifier)) return;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZoneAfter" && source.Name != "Contents") source = source.Parent;

            Device moving = (Device)e.Data.Get(Device.Identifier);
            bool copy = e.Modifiers.HasFlag(InputModifiers.Control);
            
            if (source.Name == "Contents" && e.GetPosition(source).X < source.Bounds.Width / 2) {
                if (_device.ParentIndex == 0) moving.Move(_device.Parent, copy);
                else moving.Move(_device.Parent[_device.ParentIndex.Value - 1], copy);
            } else moving.Move(_device, copy);

            e.Handled = true;
        }
    }
}
