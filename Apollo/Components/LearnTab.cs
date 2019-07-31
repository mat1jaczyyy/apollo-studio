using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Core;

namespace Apollo.Components {
    public class LearnTab: UserControl {
        public LearnTab() => AvaloniaXamlLoader.Load(this);

        void Docs() => Program.URL("https://github.com/mat1jaczyyy/apollo-studio/wiki");

        void Tutorials() => Program.URL("https://www.youtube.com/playlist?list=PLKC4R3X00beY0aB_f_ZIa3shqJX7do4mH");

        void Bug() => Program.URL("https://github.com/mat1jaczyyy/apollo-studio/issues/new?assignees=mat1jaczyyy&labels=bug&template=bug_report.md&title=");

        void Feature() => Program.URL("https://github.com/mat1jaczyyy/apollo-studio/issues/new?assignees=mat1jaczyyy&labels=enhancement&template=feature_request.md&title=");

        void Question() => Program.URL("https://github.com/mat1jaczyyy/apollo-studio/issues/new?assignees=mat1jaczyyy&labels=question&template=question.md&title=");

        void Discord() => Program.URL("https://discordapp.com/invite/2ZSHYHA");

        void Website() => Program.URL("https://apollo.mat1jaczyyy.com");

        void Patron() => Program.URL(Patreon.URL);
    }
}
