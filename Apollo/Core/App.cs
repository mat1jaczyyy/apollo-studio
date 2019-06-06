using System;

using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;

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

            if (Preferences.Theme == Preferences.Themes.Dark) Styles.Add(Dark);
            else if (Preferences.Theme == Preferences.Themes.Light) Styles.Add(Light);
        }
    }
}
