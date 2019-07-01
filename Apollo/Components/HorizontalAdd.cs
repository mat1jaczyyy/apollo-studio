using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class HorizontalAdd: AddButton {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Root = this.Get<Grid>("Root");
            Path = this.Get<Path>("Path");
        }

        public override bool AlwaysShowing {
            set {
                if (value != _always) {
                    _always = value;
                    Root.MinWidth = _always? 26 : 0;
                }
            }
        }

        public HorizontalAdd() {
            InitializeComponent();
            
            base.MouseLeave(this, null);
        }
    }
}
