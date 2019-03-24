using System;
using System.Collections.Generic;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Apollo.Core;

namespace Apollo.Components {
    public class ProjectButton: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public ProjectButton() {
            InitializeComponent();
        }

        private void Clicked(object sender, EventArgs e) {
            Program.Project.Show();
        }
    }
}
