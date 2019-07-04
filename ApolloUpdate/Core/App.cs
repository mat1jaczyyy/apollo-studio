using Avalonia;
using Avalonia.Markup.Xaml;

namespace Update.Core {
    public class App: Application {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);
    }
}