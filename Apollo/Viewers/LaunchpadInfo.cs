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
        ComboBox Rotation, InputFormatSelector;

        public LaunchpadInfo(Launchpad launchpad) {
            InitializeComponent();
            
            _launchpad = launchpad;

            this.Get<TextBlock>("Name").Text = _launchpad.Name;

            Popout = this.Get<Popout>("Popout");

            Rotation = this.Get<ComboBox>("Rotation");
            Rotation.SelectedIndex = (int)_launchpad.Rotation;

            InputFormatSelector = this.Get<ComboBox>("InputFormatSelector");
            InputFormatSelector.SelectedIndex = (int)_launchpad.InputFormat;

            if (_launchpad.GetType() == typeof(VirtualLaunchpad)) {
                Popout.IsEnabled = Rotation.IsEnabled = InputFormatSelector.IsEnabled = false;
                Popout.Opacity = Popout.Width = Rotation.Opacity = Rotation.Width = InputFormatSelector.Opacity = InputFormatSelector.Width = 0;
            }
        }

        private void Launchpad_Popout() => LaunchpadWindow.Create(_launchpad, (Window)this.GetVisualRoot());

        private void Rotation_Changed(object sender, SelectionChangedEventArgs e) => _launchpad.Rotation = (Launchpad.RotationType)Rotation.SelectedIndex;

        private void InputFormat_Changed(object sender, SelectionChangedEventArgs e) => _launchpad.InputFormat = (Launchpad.InputType)InputFormatSelector.SelectedIndex;
    }
}
