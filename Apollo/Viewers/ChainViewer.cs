using System;
using System.Globalization;
using System.Reflection;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Elements;

namespace Apollo.Viewers {
    public class ChainViewer: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        private Chain _chain;
        private Controls Contents;

        private void Contents_Insert(int index, Device device) {
            DeviceViewer viewer = new DeviceViewer(device);
            viewer.DeviceAdded += Device_Insert;
            Contents.Insert(index + 1, viewer);
        }

        public ChainViewer(Chain chain) {
            InitializeComponent();

            _chain = chain;

            Contents = this.Get<StackPanel>("Contents").Children;

            for (int i = 0; i < _chain.Count; i++)
                Contents_Insert(i, _chain[i]);
        }

        private void Device_Insert(int index, Type device) {
            _chain.Insert(index, (Device)Activator.CreateInstance(device, BindingFlags.OptionalParamBinding, null, new object[0], CultureInfo.CurrentCulture));
            Contents_Insert(index, _chain[index]);
        }

        private void Device_InsertStart(Type device) => Device_Insert(0, device);
    }
}
