using System;
using System.Collections.Generic;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using Apollo.Core;
using Apollo.Elements;

namespace Apollo.Components {
    public class Save: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public Save() {
            InitializeComponent();
        }

        private void Clicked(object sender, EventArgs e) {
            Program.Project.Save((Window)this.GetVisualRoot());
        }
    }
}
