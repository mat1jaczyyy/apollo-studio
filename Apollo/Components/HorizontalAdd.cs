using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class HorizontalAdd: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void AddedEventHandler();
        public event AddedEventHandler Added;

        Grid Root;

        private bool _always;
        public bool AlwaysShowing {
            get => _always;
            set {
                if (value != _always) {
                    _always = value;
                    Root.MinWidth = _always? 26 : 0;
                }
            }
        }

        public HorizontalAdd() {
            InitializeComponent();
            
            Root = this.Get<Grid>("Root");
        }

        private void Click(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) Added?.Invoke();
        }
    }
}
