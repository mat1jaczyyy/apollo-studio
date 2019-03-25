using System;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using Apollo.Core;

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
