using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using Apollo.Components;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Windows {
    public class LaunchpadWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        Launchpad _launchpad;
        LaunchpadGrid Grid;

        private void UpdateTopmost(bool value) => Topmost = value;

        public LaunchpadWindow(Launchpad launchpad) {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            _launchpad = launchpad;

            Title = this.Get<TextBlock>("Title").Text = _launchpad.Name;

            Grid = this.Get<LaunchpadGrid>("Grid");

            for (int i = 0; i < 100; i++)
                Grid.SetColor(LaunchpadGrid.SignalToGrid(i), new Color(0).ToScreenBrush());
            
            Grid.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated);
        }

        private void Unloaded(object sender, EventArgs e) {
            _launchpad.Window = null;

            Preferences.AlwaysOnTopChanged -= UpdateTopmost;

            if (_launchpad.GetType() == typeof(VirtualLaunchpad))
                MIDI.Disconnect(_launchpad);
        }

        public void Bounds_Updated(Rect bounds) {
            if (bounds.IsEmpty) return;

            Grid.Scale = Math.Min(bounds.Width, bounds.Height) / 200;
        }

        private void PadChanged(int index, bool state) => _launchpad.HandleMessage(new Signal(_launchpad, (byte)LaunchpadGrid.GridToSignal(index), new Color((byte)(state? 63 : 0))));
        private void PadPressed(int index) => PadChanged(index, true);
        private void PadReleased(int index) => PadChanged(index, false);

        public void SignalRender(Signal n) => Dispatcher.UIThread.InvokeAsync(() => {
            Grid.SetColor(LaunchpadGrid.SignalToGrid(n.Index), n.Color.ToScreenBrush());
        });

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();

        private void Minimize() => WindowState = WindowState.Minimized;

        private void Maximize(IPointerDevice e) {
            WindowState = (WindowState == WindowState.Maximized)? WindowState.Normal : WindowState.Maximized;
            Topmost = Preferences.AlwaysOnTop;
        }

        private void ResizeNorthWest(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.NorthWest);
        private void ResizeNorth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.North);
        private void ResizeNorthEast(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.NorthEast);
        private void ResizeWest(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.West);
        private void ResizeEast(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.East);
        private void ResizeSouthWest(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.SouthWest);
        private void ResizeSouth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.South);
        private void ResizeSouthEast(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.SouthEast);

        public static void Create(Launchpad launchpad, Window owner) {
            if (launchpad.Window == null) {
                launchpad.Window = new LaunchpadWindow(launchpad) {Owner = owner};
                launchpad.Window.Show();
                launchpad.Window.Owner = null;
            } else {
                launchpad.Window.WindowState = WindowState.Normal;
                launchpad.Window.Activate();
            }
        }
    }
}
