using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Apollo.Components {
    public class LockToggle: IconButton {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Unlocked = this.Get<Canvas>("Unlocked");
            Locked = this.Get<Canvas>("Locked");
        }

        Canvas Unlocked, Locked;

        protected override IBrush Fill {
            get => (IBrush)this.Resources["Brush"];
            set => this.Resources["Brush"] = value;
        }

        public void SetState(bool value) =>
            Unlocked.IsVisible = !(Locked.IsVisible = value);

        public LockToggle() {
            InitializeComponent();

            base.MouseLeave(this, null);

            SetState(false);
        }
    }
}
