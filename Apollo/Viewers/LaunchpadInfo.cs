using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using Apollo.Components;
using Apollo.Elements;
using Apollo.Windows;

namespace Apollo.Viewers {
    public class LaunchpadInfo: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Launchpad _launchpad;

        Popout Popout;
        ComboBox InputFormatSelector;

        public LaunchpadInfo(Launchpad launchpad) {
            InitializeComponent();
            
            _launchpad = launchpad;

            this.Get<TextBlock>("Name").Text = _launchpad.Name;

            InputFormatSelector = this.Get<ComboBox>("InputFormatSelector");
            InputFormatSelector.SelectedIndex = (int)_launchpad.InputFormat;

            Popout = this.Get<Popout>("Popout");

            if (_launchpad.GetType() == typeof(VirtualLaunchpad)) {
                InputFormatSelector.IsEnabled = Popout.IsEnabled = false;
                InputFormatSelector.Opacity = InputFormatSelector.Width = Popout.Opacity = Popout.Width = 0;
            }
        }

        private void Launchpad_Popout() => LaunchpadWindow.Create(_launchpad, (Window)this.GetVisualRoot());

        private void InputFormat_Changed(object sender, SelectionChangedEventArgs e) => _launchpad.InputFormat = (Launchpad.InputType)InputFormatSelector.SelectedIndex;
    }
}
