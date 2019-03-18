using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Elements;

namespace Apollo.Viewers {
    public class ChainViewer: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        private Chain _chain;

        public ChainViewer(Chain chain) {
            InitializeComponent();

            _chain = chain;

            Controls contents = this.Get<StackPanel>("Contents").Children;       
            for (int i = 0; i < _chain.Count; i++)
                contents.Add(new DeviceViewer(_chain[i]));
        }
    }
}
