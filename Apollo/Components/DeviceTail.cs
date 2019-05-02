using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Viewers;

namespace Apollo.Components {
    public class DeviceTail: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        DeviceViewer Owner;
        public Border Header;

        public DeviceTail(DeviceViewer owner) {
            InitializeComponent();

            Owner = owner;

            Header = this.Get<Border>("Header");
            this.Resources["TitleBrush"] = Owner.Resources["TitleBrush"];

            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);
        }

        private void Drag(object sender, PointerPressedEventArgs e) => Owner.Drag(sender, e);
        private void DragOver(object sender, DragEventArgs e) => Owner.DragOver(sender, e);
        private void Drop(object sender, DragEventArgs e) => Owner.Drop(sender, e);
    }
}
