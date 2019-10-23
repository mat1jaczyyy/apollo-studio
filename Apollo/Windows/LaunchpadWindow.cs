using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using Apollo.Components;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Structures;

namespace Apollo.Windows {
    public class LaunchpadWindow: Window {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            TitleText = this.Get<TextBlock>("Title");
            TitleCenter = this.Get<TextBlock>("TitleCenter");

            CenteringLeft = this.Get<StackPanel>("CenteringLeft");
            CenteringRight = this.Get<StackPanel>("CenteringRight");

            Grid = this.Get<LaunchpadGrid>("Grid");
        }

        Launchpad _launchpad;
        LaunchpadGrid Grid;
        
        TextBlock TitleText, TitleCenter;
        StackPanel CenteringLeft, CenteringRight;

        void UpdateTopmost(bool value) => Topmost = value;

        public LaunchpadWindow() => new InvalidOperationException();

        public LaunchpadWindow(Launchpad launchpad) {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            _launchpad = launchpad;

            Title = TitleText.Text = TitleCenter.Text = _launchpad.Name;

            for (int i = 0; i < 100; i++)
                Grid.SetColor(LaunchpadGrid.SignalToGrid(i), launchpad.GetColor(i).ToScreenBrush());
            
            Grid.GetObservable(Visual.BoundsProperty).Subscribe(Grid_Updated);

            this.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated);
            TitleText.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated);
            TitleCenter.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated);
            CenteringLeft.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated);
            CenteringRight.GetObservable(Visual.BoundsProperty).Subscribe(Bounds_Updated);
        }

        void Unloaded(object sender, CancelEventArgs e) {
            _launchpad.Window = null;

            Preferences.AlwaysOnTopChanged -= UpdateTopmost;

            if (_launchpad.GetType() == typeof(VirtualLaunchpad))
                MIDI.Disconnect(_launchpad);
            
            _launchpad = null;

            this.Content = null;
        }

        public void Bounds_Updated(Rect bounds) {
            if (Bounds.IsEmpty || TitleText.Bounds.IsEmpty || TitleCenter.Bounds.IsEmpty || CenteringLeft.Bounds.IsEmpty || CenteringRight.Bounds.IsEmpty) return;

            int result = Convert.ToInt32((Bounds.Width - TitleText.Bounds.Width) / 2 <= Math.Max(CenteringLeft.Bounds.Width, CenteringRight.Bounds.Width) + 10);

            TitleText.Opacity = result;
            TitleCenter.Opacity = 1 - result;
        }

        public void Grid_Updated(Rect bounds) {
            if (bounds.IsEmpty) return;

            Grid.Scale = Math.Min(bounds.Width, bounds.Height) / 200;
        }

        void PadChanged(int index, bool state) {
            Signal n = new Signal(_launchpad, _launchpad, (byte)LaunchpadGrid.GridToSignal(index), new Color((byte)(state? 127 : 0)));

            if (_launchpad is AbletonLaunchpad abletonLaunchpad)
                AbletonConnector.Send(abletonLaunchpad, n);

            _launchpad.HandleMessage(n, true);
        }

        void PadPressed(int index) => PadChanged(index, true);
        void PadReleased(int index) => PadChanged(index, false);

        public void SignalRender(Signal n) => Dispatcher.UIThread.InvokeAsync(() => {
            Grid.SetColor(LaunchpadGrid.SignalToGrid(n.Index), n.Color.ToScreenBrush());
        });

        async void HandleKey(object sender, KeyEventArgs e) {
            if (App.Dragging) return;

            if (!App.WindowKey(this, e) && Program.Project != null && !await Program.Project.HandleKey(this, e))
                Program.Project?.Undo.HandleKey(e);
        }

        void Window_KeyDown(object sender, KeyEventArgs e) {
            List<Window> windows = App.Windows.ToList();
            HandleKey(sender, e);
            
            if (windows.SequenceEqual(App.Windows) && FocusManager.Instance.Current?.GetType() != typeof(TextBox))
                this.Focus();
        }

        void MoveWindow(object sender, PointerPressedEventArgs e) {
            if (e.ClickCount == 2) Maximize(null);
            else BeginMoveDrag(e);

            Topmost = false;
            Topmost = Preferences.AlwaysOnTop;
            Activate();
        }

        void Minimize() => WindowState = WindowState.Minimized;

        void Maximize(PointerEventArgs e) {
            WindowState = (WindowState == WindowState.Maximized)? WindowState.Normal : WindowState.Maximized;

            Topmost = false;
            Topmost = Preferences.AlwaysOnTop;
            Activate();
        }

        void ResizeNorthWest(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.NorthWest, e);
        void ResizeNorth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.North, e);
        void ResizeNorthEast(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.NorthEast, e);
        void ResizeWest(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.West, e);
        void ResizeEast(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.East, e);
        void ResizeSouthWest(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.SouthWest, e);
        void ResizeSouth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.South, e);
        void ResizeSouthEast(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.SouthEast, e);

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
