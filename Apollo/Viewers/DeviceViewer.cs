using System;
using System.Linq;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Elements;

namespace Apollo.Viewers {
    public class DeviceViewer: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
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

            _device = device;
            _viewer = GetSpecificViewer(_device);

            if (_viewer != null)
                this.Get<Grid>("Contents").Children.Add(_viewer);
        }
    }
}
