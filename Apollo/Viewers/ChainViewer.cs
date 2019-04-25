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
    public class ChainViewer: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Chain _chain;

        Controls Contents;
        DeviceAdd DeviceAdd;

        int? selected = null;

        public void Contents_Insert(int index, Device device) {
            DeviceViewer viewer = new DeviceViewer(device);
            viewer.DeviceAdded += Device_Insert;
            viewer.DeviceRemoved += Device_Remove;

            Contents.Insert(index + 1, viewer);
            DeviceAdd.AlwaysShowing = false;

            if (selected != null && index <= selected) selected++;
        }

        public void Contents_Remove(int index) {
            Contents.RemoveAt(index + 1);
            if (Contents.Count == 1) DeviceAdd.AlwaysShowing = true;

            if (selected != null) {
                if (index < selected) selected--;
                else if (index == selected) Select(null);
            }
        }

        public void Select(int? index) {
            if (selected != null) {
                _chain[selected.Value].Viewer.Deselect();

                if (index == selected) {
                    selected = null;
                    return;
                }
            }

            if (index != null) {
                _chain[index.Value].Viewer.Select();
            }
            
            selected = index;
        }

        public ChainViewer(Chain chain, bool backgroundBorder = false) {
            InitializeComponent();

            _chain = chain;
            _chain.Viewer = this;
            
            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);

            Contents = this.Get<StackPanel>("Contents").Children;
            DeviceAdd = this.Get<DeviceAdd>("DeviceAdd");

            for (int i = 0; i < _chain.Count; i++)
                Contents_Insert(i, _chain[i]);
            
            if (backgroundBorder) {
                this.Get<Grid>("Root").Children.Insert(0, new DeviceBackground());
                Background = (IBrush)Application.Current.Styles.FindResource("ThemeControlDarkenBrush");
            }
        }

        private void Device_Insert(int index, Type device) {
            _chain.Insert(index, Device.Create(device, _chain));
            Contents_Insert(index, _chain[index]);
            
            Select(index);
        }

        private void Device_InsertStart(Type device) => Device_Insert(0, device);

        private void Device_Remove(int index) {
            _chain.Remove(index);
            Contents_Remove(index);
        }

        private void DragOver(object sender, DragEventArgs e) {
            if (!e.Data.Contains(Device.Identifier)) e.DragEffects = DragDropEffects.None; 
        }

        private void Drop(object sender, DragEventArgs e) {
            if (!e.Data.Contains(Device.Identifier)) return;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZoneBefore" && source.Name != "DropZoneAfter" && source.Name != "DeviceAdd") source = source.Parent;

            Device moving = (Device)e.Data.Get(Device.Identifier);
            bool result;

            if (source.Name != "DropZoneAfter") result = moving.Move(_chain);
            else result = moving.Move(_chain.Devices.Last());

            if (!result) e.DragEffects = DragDropEffects.None;
            
            e.Handled = true;
        }
    }
}
