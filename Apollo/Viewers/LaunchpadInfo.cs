using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Windows;

namespace Apollo.Viewers {
    public class LaunchpadInfo: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Launchpad _launchpad;
        ComboBox InputFormatSelector;

        public LaunchpadInfo(Launchpad launchpad) {
            InitializeComponent();
            
            _launchpad = launchpad;

            this.Get<TextBlock>("Name").Text = _launchpad.Name;

            InputFormatSelector = this.Get<ComboBox>("InputFormatSelector");
            InputFormatSelector.SelectedIndex = (int)_launchpad.InputFormat;
        }

        private void InputFormat_Changed(object sender, SelectionChangedEventArgs e) {

        }
    }
}
