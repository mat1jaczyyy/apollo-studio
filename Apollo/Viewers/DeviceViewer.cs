using System;
using System.Linq;
using System.Reflection;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Elements;

namespace Apollo.Viewers {
    public class DeviceViewer: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void DeviceAddedEventHandler(int index, Type device);
        public event DeviceAddedEventHandler DeviceAdded;
        
        IControl GetSpecificViewer(Device device) {
            foreach (Type deviceViewer in (from type in Assembly.GetExecutingAssembly().GetTypes() where type.Namespace.StartsWith("Apollo.DeviceViewers") select type)) {       
                if ((string)deviceViewer.GetField("DeviceIdentifier").GetValue(null) == device.DeviceIdentifier)
                    return (IControl)Activator.CreateInstance(deviceViewer, new object[] {device});
            }

            return null;
        }

        private Device _device;
        private IControl _viewer;

        public DeviceViewer(Device device) {
            InitializeComponent();

            this.Get<TextBlock>("Title").Text = device.GetType().ToString().Split(".").Last();

            _device = device;
            _viewer = GetSpecificViewer(_device);

            if (_viewer != null)
                this.Get<Grid>("Contents").Children.Add(_viewer);
        }

        private void Device_Add(Type device) {
            DeviceAdded?.Invoke(_device.ParentIndex.Value + 1, device);
        }

        private void Device_Remove(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) {
                ((Panel)Parent).Children.RemoveAt(_device.ParentIndex.Value + 1);
                _device.Parent.Remove(_device.ParentIndex.Value);
            }
        }
    }
}
