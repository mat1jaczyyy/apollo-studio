using System;
using System.Collections.Generic;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Windows;

namespace Apollo.Components {
    public class PreferencesButton: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public PreferencesButton() {
            InitializeComponent();
        }

        private void Clicked(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left)
                if (Program.PreferencesWindow == null) {
                    new PreferencesWindow().Show();
                } else {
                    Program.PreferencesWindow.WindowState = WindowState.Normal;
                    Program.PreferencesWindow.Activate();
                }
        }
    }
}
