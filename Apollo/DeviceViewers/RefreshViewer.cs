using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.DeviceViewers {
    public class RefreshViewer: UserControl {
        public static readonly string DeviceIdentifier = "refresh";

        void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Refresh _refresh;
        StackPanel MacroStack = new StackPanel();

        public RefreshViewer() => new InvalidOperationException();

        public RefreshViewer(Refresh refresh) {
            InitializeComponent();

            _refresh = refresh;
        }

        void Macro_Changed(object sender, RoutedEventArgs e) {
            CheckBox source = (CheckBox)sender;
            int index = ((StackPanel)source.Parent).Children.IndexOf(source);
            bool value = source.IsChecked.Value;

            if (_refresh.GetMacro(index) != value) {
                bool u = value;
                bool r = _refresh.GetMacro(index);
                List<int> path = Track.GetPath(_refresh);

                Program.Project.Undo.Add($"Refresh Macro {index} changed to {(r? "Enabled" : "Disabled")}", () => {
                    Track.TraversePath<Refresh>(path).SetMacro(index, u);
                }, () => {
                    Track.TraversePath<Refresh>(path).SetMacro(index, r);
                });

                SetMacro(index, value);
            }
        }

        public void SetMacro(int index, bool value) => _refresh.SetMacro(index, value);

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _refresh = null;
    }
}
