using Avalonia;
using Avalonia.Markup.Xaml;

namespace api {
    public class App: Application {
        public override void Initialize() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
