using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Apollo.Components {
    public class UpdateButton: IconButton {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Message = this.Get<TextBlock>("Message");
        }

        TextBlock Message;

        protected override IBrush Fill { get; set; }

        public UpdateButton() {
            InitializeComponent();

            base.MouseLeave(this, null);
        }

        public void Enable(string message) {
            Message.Text = message;
            IsVisible = true;
        }
    }
}
