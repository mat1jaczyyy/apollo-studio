using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using Apollo.Components;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.DeviceViewers {
    public class PreviewViewer: UserControl {
        public static readonly string DeviceIdentifier = "preview";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Grid = this.Get<LaunchpadGrid>("Grid");
        }
        
        Preview _preview;
        LaunchpadGrid Grid;

        public PreviewViewer(Preview preview) {
            InitializeComponent();

            _preview = preview;
            _preview.SignalExited += SignalRender;

            for (int i = 0; i < 100; i++)
                Grid.SetColor(LaunchpadGrid.SignalToGrid(i), new Color(0).ToScreenBrush());
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _preview = null;

        void PadChanged(int index, bool state) => _preview.MIDIEnter(new Signal(Track.Get(_preview)?.Launchpad, (byte)LaunchpadGrid.GridToSignal(index), new Color((byte)(state? 63 : 0))));
        void PadPressed(int index) => PadChanged(index, true);
        void PadReleased(int index) => PadChanged(index, false);

        void SignalRender(Signal n) => Dispatcher.UIThread.InvokeAsync(() => {
            Grid.SetColor(LaunchpadGrid.SignalToGrid(n.Index), n.Color.ToScreenBrush());
        });
    }
}
