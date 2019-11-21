using System;

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

        public PreviewViewer() => new InvalidOperationException();

        public PreviewViewer(Preview preview) {
            InitializeComponent();

            _preview = preview;

            for (int i = 0; i < 100; i++)
                Grid.SetColor(LaunchpadGrid.SignalToGrid(i), new Color(0).ToScreenBrush());
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _preview = null;

        void PadChanged(int index, bool state) {
            Launchpad lp = Track.Get(_preview)?.Launchpad;
            _preview.MIDIEnter(new Signal(lp, lp, (byte)LaunchpadGrid.GridToSignal(index), new Color((byte)(state? 63 : 0))));
        }

        void PadPressed(int index) => PadChanged(index, true);
        void PadReleased(int index) => PadChanged(index, false);

        public void Signal(Signal n) => Dispatcher.UIThread.InvokeAsync(() => {
            Grid.SetColor(LaunchpadGrid.SignalToGrid(n.Index), n.Color.ToScreenBrush());
        });

        public void Clear() => Dispatcher.UIThread.InvokeAsync(() => {
            Grid.Clear();
        });
    }
}
