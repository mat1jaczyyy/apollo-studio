using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using UniformGrid = Avalonia.Controls.Primitives.UniformGrid;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Input;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.DeviceViewers {
    public class PageFilterViewer: UserControl {
        public static readonly string DeviceIdentifier = "pagefilter";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        PageFilter _filter;
        UniformGrid PagesGrid;

        private void Set(Rectangle rect, bool value) => rect.Fill = (IBrush)Application.Current.Styles.FindResource(value? "ThemeExtraBrush" : "ThemeForegroundLowBrush");

        public PageFilterViewer(PageFilter filter) {
            InitializeComponent();

            _filter = filter;

            PagesGrid = this.Get<UniformGrid>("PagesGrid");

            for (int i = 0; i < PagesGrid.Children.Count; i++)
                Set((Rectangle)PagesGrid.Children[i], _filter[i]);
        }

        private void Clicked(object sender, PointerReleasedEventArgs e) {
            int index = PagesGrid.Children.IndexOf((IControl)sender);

            bool u = _filter[index];
            bool r = !_filter[index];
            List<int> path = Track.GetPath(_filter);

            Program.Project.Undo.Add($"PageFilter Changed", () => {
                ((PageFilter)Track.TraversePath(path))[index] = u;
            }, () => {
                ((PageFilter)Track.TraversePath(path))[index] = r;
            });

            _filter[index] = !_filter[index];
        }

        public void Set(int index, bool value) => Set((Rectangle)PagesGrid.Children[index], value);
    }
}
