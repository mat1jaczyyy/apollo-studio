using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class TrackAdd: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void TrackAddedEventHandler();
        public event TrackAddedEventHandler TrackAdded;
        
        private bool _always;
        public bool AlwaysShowing {
            get => _always;
            set {
                if (value != _always) {
                    _always = value;
                    this.Get<Grid>("Root").MinHeight = _always? 30 : 0;
                }
            }
        }

        public TrackAdd() => InitializeComponent();

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) TrackAdded?.Invoke();
        }
    }
}
