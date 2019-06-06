using System;

using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;

namespace Apollo.Core {
    public class App: Application {
        public override void Initialize() {
            AvaloniaXamlLoader.Load(this);

            StyleInclude dark = new StyleInclude(new Uri("resm:Styles?assembly=Apollo")) {
                Source = new Uri("avares://Apollo/Themes/ThemeLight.xaml")
            };

            Styles.Add(dark);
        }
    }
}
