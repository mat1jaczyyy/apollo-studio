using System;

using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;

using Apollo.Enums;

namespace Apollo.Core {
    public class App: Application {
        StyleInclude Dark = new StyleInclude(new Uri("resm:Styles?assembly=Apollo")) {
            Source = new Uri("avares://Apollo/Themes/Dark.xaml")
        };

        StyleInclude Light = new StyleInclude(new Uri("resm:Styles?assembly=Apollo")) {
            Source = new Uri("avares://Apollo/Themes/Light.xaml")
        };

        public override void Initialize() {
            AvaloniaXamlLoader.Load(this);

            if (Preferences.Theme == Themes.Dark) Styles.Add(Dark);
            else if (Preferences.Theme == Themes.Light) Styles.Add(Light);
        }
    }
}
