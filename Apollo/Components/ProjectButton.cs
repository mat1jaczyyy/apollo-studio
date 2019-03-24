using System;
using System.Collections.Generic;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Apollo.Core;
using Apollo.Windows;

namespace Apollo.Components {
    public class ProjectButton: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public ProjectButton() {
            InitializeComponent();
        }

        private void Clicked(object sender, EventArgs e) {
            if (Program.Project.Window == null) {
                new ProjectWindow().Show();
            } else {
                Program.Project.Window.Activate();
            }
        }
    }
}
