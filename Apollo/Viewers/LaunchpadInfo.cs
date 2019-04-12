using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Elements;

namespace Apollo.Viewers {
    public class LaunchpadInfo: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Launchpad _launchpad;
        ComboBox InputFormatSelector;

        public LaunchpadInfo(Launchpad launchpad) {
            InitializeComponent();
            
            _launchpad = launchpad;

            this.Get<TextBlock>("Name").Text = _launchpad.Name;

            InputFormatSelector = this.Get<ComboBox>("InputFormatSelector");
            InputFormatSelector.SelectedIndex = (int)_launchpad.InputFormat;
        }

        private void InputFormat_Changed(object sender, SelectionChangedEventArgs e) => _launchpad.InputFormat = (Launchpad.InputType)InputFormatSelector.SelectedIndex;
    }
}
