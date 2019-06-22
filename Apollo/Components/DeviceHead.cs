using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Apollo.Elements;
using Apollo.Viewers;

namespace Apollo.Components {
    public class DeviceHead: UserControl {
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Border = this.Get<Border>("Border");
            Header = this.Get<Border>("Header");
        }

        DeviceViewer Owner;
        public Border Border, Header;

        public DeviceHead(Device owner, DeviceViewer ownerviewer) {
            InitializeComponent();

            Owner = ownerviewer;

            this.Resources["TitleBrush"] = Owner.Header.Background?? Owner.Resources["TitleBrush"];

            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);

            SetEnabled(owner.Enabled);
        }

        private void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            Owner = null;

            this.RemoveHandler(DragDrop.DropEvent, Drop);
            this.RemoveHandler(DragDrop.DragOverEvent, DragOver);
        }

        public void SetEnabled(bool value) {
            Border.Background = (IBrush)Application.Current.Styles.FindResource(value? "ThemeControlHighBrush" : "ThemeControlMidBrush");
            Border.BorderBrush = (IBrush)Application.Current.Styles.FindResource(value? "ThemeBorderMidBrush" : "ThemeBorderLowBrush");
        }

        private void Drag(object sender, PointerPressedEventArgs e) => Owner.Drag(sender, e);
        private void DragOver(object sender, DragEventArgs e) => Owner.DragOver(sender, e);
        private void Drop(object sender, DragEventArgs e) => Owner.Drop(sender, e);
    }
}
