using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using UniformGrid = Avalonia.Controls.Primitives.UniformGrid;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Input;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.DeviceViewers {
    public class PageFilterViewer: UserControl {
        public static readonly string DeviceIdentifier = "pagefilter";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            PagesGrid = this.Get<UniformGrid>("PagesGrid");
        }
        
        PageFilter _filter;
        UniformGrid PagesGrid;

        void Set(PageRectangle rect, bool value) => rect.Fill = (IBrush)Application.Current.Styles.FindResource(value? "ThemeExtraBrush" : "ThemeForegroundLowBrush");

        public PageFilterViewer(PageFilter filter) {
            InitializeComponent();

            _filter = filter;

            for (int i = 0; i < PagesGrid.Children.Count; i++) {
                PageRectangle Rect = (PageRectangle)PagesGrid.Children[i];
                Set(Rect, _filter[i]);
                Rect.Index = i + 1;
            }
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _filter = null;

        void Clicked(object sender, PointerReleasedEventArgs e) {
            int index = PagesGrid.Children.IndexOf((IControl)sender);

            bool u = _filter[index];
            bool r = !_filter[index];
            List<int> path = Track.GetPath(_filter);

            Program.Project.Undo.Add($"PageFilter {index + 1} Changed to {(r? "Allowed" : "Blocked")}", () => {
                ((PageFilter)Track.TraversePath(path))[index] = u;
            }, () => {
                ((PageFilter)Track.TraversePath(path))[index] = r;
            });

            _filter[index] = !_filter[index];
        }

        public void Set(int index, bool value) => Set((PageRectangle)PagesGrid.Children[index], value);
    }
}
