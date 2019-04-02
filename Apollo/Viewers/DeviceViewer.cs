using System;
using System.Linq;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Elements;

namespace Apollo.Viewers {
    public class DeviceViewer: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public static IControl GetSpecificViewer(DeviceViewer sender, Device device) {
            foreach (Type deviceViewer in (from type in Assembly.GetExecutingAssembly().GetTypes() where type.Namespace.StartsWith("Apollo.DeviceViewers") select type))      
                if ((string)deviceViewer.GetField("DeviceIdentifier").GetValue(null) == device.DeviceIdentifier) {
                    if (device.DeviceIdentifier == "group") {
                        sender.Get<Border>("Border").CornerRadius = new CornerRadius(5, 0, 0, 5);
                        sender.Get<Grid>("Contents").Margin = new Thickness(0);
                        return (IControl)Activator.CreateInstance(deviceViewer, new object[] {device, sender.Get<StackPanel>("Root")});
                    }

                    return (IControl)Activator.CreateInstance(deviceViewer, new object[] {device});
                }

            return null;
        }

        public delegate void DeviceAddedEventHandler(int index, Type device);
        public event DeviceAddedEventHandler DeviceAdded;
        
        private Device _device;
        private IControl _viewer;

        public DeviceViewer(Device device) {
            InitializeComponent();

            this.Get<TextBlock>("Title").Text = device.GetType().ToString().Split(".").Last();

            _device = device;
            _viewer = GetSpecificViewer(this, _device);

            if (_viewer != null)
                this.Get<Grid>("Contents").Children.Add(_viewer);
        }

        private void Device_Add(Type device) => DeviceAdded?.Invoke(_device.ParentIndex.Value + 1, device);

        private void Device_Remove() {
            ((Panel)Parent).Children.RemoveAt(_device.ParentIndex.Value + 1);
            _device.Parent.Remove(_device.ParentIndex.Value);
        }
    }
}
