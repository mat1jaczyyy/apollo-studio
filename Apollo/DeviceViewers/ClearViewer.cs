using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Enums;

namespace Apollo.DeviceViewers {
    public class ClearViewer: UserControl {
        public static readonly string DeviceIdentifier = "clear";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            ClearMode = this.Get<ComboBox>("ClearMode");
        }
        
        Clear _clear;
        ComboBox ClearMode;

        public ClearViewer(Clear clear) {
            InitializeComponent();

            _clear = clear;

            ClearMode.SelectedIndex = (int)_clear.Mode;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _clear = null;

        void Mode_Changed(object sender, SelectionChangedEventArgs e) {
            ClearType selected = (ClearType)ClearMode.SelectedIndex;

            if (_clear.Mode != selected) {
                ClearType u = _clear.Mode;
                ClearType r = selected;
                List<int> path = Track.GetPath(_clear);

                Program.Project.Undo.Add($"Clear Orientation Changed to {((ComboBoxItem)ClearMode.ItemContainerGenerator.ContainerFromIndex((int)r)).Content}", () => {
                    ((Clear)Track.TraversePath(path)).Mode = u;
                }, () => {
                    ((Clear)Track.TraversePath(path)).Mode = r;
                });

                _clear.Mode = selected;
            }
        }

        public void SetMode(ClearType mode) => ClearMode.SelectedIndex = (int)mode;
    }
}
