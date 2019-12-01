using System;
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
    public class MacroFilterViewer: UserControl {
        public static readonly string DeviceIdentifier = "macrofilter";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            MacrosGrid = this.Get<UniformGrid>("MacrosGrid");
            MacroDial = this.Get<Dial>("MacroDial");
        }
        
        MacroFilter _filter;
        UniformGrid MacrosGrid;
        Dial MacroDial;

        void Set(MacroRectangle rect, bool value) => rect.Fill = (IBrush)Application.Current.Styles.FindResource(value? "ThemeExtraBrush" : "ThemeForegroundLowBrush");

        public MacroFilterViewer() => new InvalidOperationException();

        public MacroFilterViewer(MacroFilter filter) {
            InitializeComponent();

            _filter = filter;
            
            MacroDial.RawValue = _filter.Macro;

            for (int i = 0; i < MacrosGrid.Children.Count; i++) {
                MacroRectangle Rect = (MacroRectangle)MacrosGrid.Children[i];
                Set(Rect, _filter[i]);
                Rect.Index = i + 1;
            }
        }
        
        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _filter = null;

        void Target_Changed(Dial sender, double value, double? old){
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_filter);

                Program.Project.Undo.Add($"MacroFilter Target Changed to {r}", () => {
                    ((MacroFilter)Track.TraversePath(path)).Macro = u;
                }, () => {
                    ((MacroFilter)Track.TraversePath(path)).Macro = r;
                });
            }

            _filter.Macro = (int)value;
        }
        
        public void SetMacro(int macro) => MacroDial.RawValue = macro;

        void Clicked(object sender, PointerReleasedEventArgs e) {
            int index = MacrosGrid.Children.IndexOf((IControl)sender);

            bool u = _filter[index];
            bool r = !_filter[index];
            List<int> path = Track.GetPath(_filter);

            Program.Project.Undo.Add($"MacroFilter {index + 1} Changed to {(r? "Allowed" : "Blocked")}", () => {
                ((MacroFilter)Track.TraversePath(path))[index] = u;
            }, () => {
                ((MacroFilter)Track.TraversePath(path))[index] = r;
            });

            _filter[index] = !_filter[index];
        }

        public void Set(int index, bool value) => Set((MacroRectangle)MacrosGrid.Children[index], value);
    }
}
