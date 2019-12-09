using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.DeviceViewers {
    public class KeyFilterViewer: UserControl {
        public static readonly string DeviceIdentifier = "keyfilter";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Grid = this.Get<LaunchpadGrid>("Grid");
        }
        
        KeyFilter _filter;
        LaunchpadGrid Grid;

        SolidColorBrush GetColor(bool value) => (SolidColorBrush)Application.Current.Styles.FindResource(value? "ThemeAccentBrush" : "ThemeForegroundLowBrush");

        public KeyFilterViewer() => new InvalidOperationException();

        public KeyFilterViewer(KeyFilter filter) {
            InitializeComponent();

            _filter = filter;

            for (int i = 0; i < 101; i++)
                Grid.SetColor(LaunchpadGrid.SignalToGrid(i), GetColor(_filter[i]));
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _filter = null;

        bool drawingState;
        bool[] old;
        
        void PadStarted(int index) {
            drawingState = !_filter[LaunchpadGrid.GridToSignal(index)];
            old = _filter.Filter.ToArray();
        }

        void PadPressed(int index) => Grid.SetColor(index, GetColor(_filter[LaunchpadGrid.GridToSignal(index)] = drawingState));

        void PadFinished(int index) {
            if (old == null) return;

            bool[] u = old.ToArray();
            bool[] r = _filter.Filter.ToArray();
            List<int> path = Track.GetPath(_filter);

            Program.Project.Undo.Add($"KeyFilter Changed", () => {
                Track.TraversePath<KeyFilter>(path).Filter = u.ToArray();
            }, () => {
                Track.TraversePath<KeyFilter>(path).Filter = r.ToArray();
            });

            old = null;
        }

        public void Set(bool[] filter) {
            for (int i = 0; i < 100; i++)
                Grid.SetColor(LaunchpadGrid.SignalToGrid(i), GetColor(filter[i]));
        }
    }
}
