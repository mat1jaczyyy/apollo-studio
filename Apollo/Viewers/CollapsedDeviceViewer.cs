using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Apollo.Components;
using Apollo.Elements;

namespace Apollo.Viewers {
    public class CollapsedDeviceViewer: DeviceViewer {
        protected override void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        protected override void ApplyHeaderBrush(string resource) {
            IBrush brush = (IBrush)Application.Current.Styles.FindResource(resource);

            if (IsArrangeValid) Header.Background = brush;
            else this.Resources["TitleBrush"] = brush;
        }

        public CollapsedDeviceViewer(Device device) {
            TitleText = this.Get<TextBlock>("Title");
            TitleText.Text = device.GetType().ToString().Split(".").Last();

            _device = device;
            _device.Viewer = this;

            Root = this.Get<StackPanel>("Root");

            Header = this.Get<Border>("Contents");
            Deselect();
            
            DeviceAdd = this.Get<DeviceAdd>("DropZoneAfter");

            DeviceContextMenu = (ContextMenu)this.Resources["DeviceContextMenu"];
            GroupContextMenu = (ContextMenu)this.Resources["GroupContextMenu"];
            
            DeviceContextMenu.AddHandler(MenuItem.ClickEvent, ContextMenu_Click);
            GroupContextMenu.AddHandler(MenuItem.ClickEvent, ContextMenu_Click);
            
            Draggable = this.Get<Grid>("Draggable");
            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);

            SetEnabled();
        }

        protected override void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            base.ResetEvents();

            SpecificViewer = null;
            _device.Viewer = null;
            _device = null;

            DeviceContextMenu.RemoveHandler(MenuItem.ClickEvent, ContextMenu_Click);
            GroupContextMenu.RemoveHandler(MenuItem.ClickEvent, ContextMenu_Click);
            DeviceContextMenu = GroupContextMenu = null;
            
            this.RemoveHandler(DragDrop.DropEvent, Drop);
            this.RemoveHandler(DragDrop.DragOverEvent, DragOver);
        }

        public override void SetEnabled() {
            Header.BorderBrush = (IBrush)Application.Current.Styles.FindResource(_device.Enabled? "ThemeBorderMidBrush" : "ThemeBorderLowBrush");
            TitleText.Foreground = (IBrush)Application.Current.Styles.FindResource(_device.Enabled? "ThemeForegroundBrush" : "ThemeForegroundLowBrush");
        }
    }
}
