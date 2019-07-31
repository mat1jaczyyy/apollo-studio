using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class LearnButton: UserControl {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            Grid = this.Get<Grid>("Grid");
            TextBlock = this.Get<TextBlock>("TextBlock");
        }

        public delegate void ClickEventHandler();
        public event ClickEventHandler Click;

        Grid Grid;
        TextBlock TextBlock;

        public Canvas Icon {
            get => (Grid.Children.Count > 1)? (Canvas)Grid.Children[1] : null;
            set {
                Grid.SetColumn(value, 1);
                
                if (Grid.Children.Count > 1) Grid.Children[1] = value;
                else Grid.Children.Add(value);
            }
        }

        public string Text {
            get => TextBlock.Text;
            set => TextBlock.Text = value;
        }

        public LearnButton() => InitializeComponent();

        void Clicked(object sender, RoutedEventArgs e) => Click?.Invoke();
    }
}
