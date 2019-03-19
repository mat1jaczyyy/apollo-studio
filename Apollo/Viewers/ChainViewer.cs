using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Elements;

namespace Apollo.Viewers {
    public class ChainViewer: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        private Chain _chain;
        private Controls Contents;

        public ChainViewer(Chain chain) {
            InitializeComponent();

            _chain = chain;

            Contents = this.Get<StackPanel>("Contents").Children;

            for (int i = 0; i < _chain.Count; i++)
                Contents.Add(new DeviceViewer(_chain[i]));
        }

        private void Device_Add(Type device) {
            _chain.Insert(0, (Device)Activator.CreateInstance(device, BindingFlags.OptionalParamBinding, null, new object[0], CultureInfo.CurrentCulture));
            Contents.Insert(1, new DeviceViewer(_chain[0]));
        }
    }
}
