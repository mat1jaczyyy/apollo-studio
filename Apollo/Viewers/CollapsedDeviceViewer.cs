using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Apollo.Components;
using Apollo.Elements;
using Apollo.Selection;

namespace Apollo.Viewers {
    public class CollapsedDeviceViewer: DeviceViewer {
        protected override void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Root = this.Get<StackPanel>("Root");
            Header = this.Get<Border>("Contents");
            
            Draggable = this.Get<Grid>("Draggable");
            DeviceAdd = this.Get<DeviceAdd>("DropZoneAfter");
            Indicator = this.Get<Indicator>("Indicator");

            TitleText = this.Get<TextBlock>("Title");

            DeviceMute = this.Get<MenuItem>("DeviceMute");
            GroupMute = this.Get<MenuItem>("GroupMute");
            ChokeMute = this.Get<MenuItem>("ChokeMute");
        }

        protected override void ApplyHeaderBrush(string resource) {
            IBrush brush = (IBrush)Application.Current.Styles.FindResource(resource);

            if (IsArrangeValid) Header.Background = brush;
            else this.Resources["TitleBrush"] = brush;
        }

        public CollapsedDeviceViewer() => new InvalidOperationException();

        public CollapsedDeviceViewer(Device device) {
            TitleText.Text = device.Name;

            _device = device;
            _device.Viewer = this;
            Deselect();

            DeviceContextMenu = (ApolloContextMenu)this.Resources["DeviceContextMenu"];
            GroupContextMenu = (ApolloContextMenu)this.Resources["GroupContextMenu"];
            ChokeContextMenu = (ApolloContextMenu)this.Resources["ChokeContextMenu"];
            
            DragDrop = new DragDropManager(this);

            SetEnabled();
        }

        protected override void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            base.ResetEvents();

            SpecificViewer = null;
            _device.Viewer = null;
            _device = null;

            DeviceContextMenu = GroupContextMenu = ChokeContextMenu = null;

            DragDrop.Dispose();
            DragDrop = null;
        }

        public override void SetEnabled() {
            Header.BorderBrush = (IBrush)Application.Current.Styles.FindResource(_device.Enabled? "ThemeBorderMidBrush" : "ThemeBorderLowBrush");
            TitleText.Foreground = (IBrush)Application.Current.Styles.FindResource(_device.Enabled? "ThemeForegroundBrush" : "ThemeForegroundLowBrush");
        }
    }
}
