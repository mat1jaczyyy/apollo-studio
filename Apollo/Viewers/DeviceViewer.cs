using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Interfaces;

namespace Apollo.Viewers {
    public class DeviceViewer: UserControl, ISelectViewer {
        protected virtual void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            TitleText = this.Get<TextBlock>("Title");
            Root = this.Get<StackPanel>("Root");
            Border = this.Get<Border>("Border");
            Header = this.Get<Border>("Header");
            
            DeviceAdd = this.Get<DeviceAdd>("DropZoneAfter");
            Draggable = this.Get<Grid>("Draggable");

            Indicator = this.Get<Indicator>("Indicator");

            DeviceMute = this.Get<MenuItem>("DeviceMute");
            GroupMute = this.Get<MenuItem>("DeviceMute");
        }

        static IControl GetSpecificViewer(DeviceViewer sender, Device device) {
            foreach (Type deviceViewer in (from type in Assembly.GetExecutingAssembly().GetTypes() where type.ReflectedType == null && type.Namespace.StartsWith("Apollo.DeviceViewers") select type))
                if ((string)deviceViewer.GetField("DeviceIdentifier").GetValue(null) == device.DeviceIdentifier) {
                    if (device.DeviceIdentifier == "group" || device.DeviceIdentifier == "multi" || device.DeviceIdentifier == "choke")
                        return (IControl)Activator.CreateInstance(deviceViewer, new object[] {device, sender});
                    
                    return (IControl)Activator.CreateInstance(deviceViewer, new object[] {device});
                }

            return null;
        }

        public delegate void AddedEventHandler(int index, Type device);
        public event AddedEventHandler Added;

        public delegate void DeviceCollapsedEventHandler(int index);
        public event DeviceCollapsedEventHandler DeviceCollapsed;

        protected void ResetEvents() {
            Added = null;
            DeviceCollapsed = null;
        }
        
        protected Device _device;
        protected bool selected = false;

        public IControl SpecificViewer { get; protected set; }

        public StackPanel Root;
        public Border Border, Header;
        public DeviceAdd DeviceAdd { get; protected set; }
        public Indicator Indicator { get; protected set; }

        protected TextBlock TitleText;
        protected Grid Draggable;
        protected ContextMenu DeviceContextMenu, GroupContextMenu;
        protected MenuItem DeviceMute, GroupMute;

        protected virtual void ApplyHeaderBrush(string resource) {
            IBrush brush = (IBrush)Application.Current.Styles.FindResource(resource);

            if (Root.Children[0].GetType() == typeof(DeviceHead)) {
                DeviceHead target = ((DeviceHead)Root.Children[0]);

                if (IsArrangeValid) target.Header.Background = brush;
                else target.Resources["TitleBrush"] = brush;
            }

            if (Root.Children[Root.Children.Count - 2].GetType() == typeof(DeviceTail)) {
                DeviceTail target = ((DeviceTail)Root.Children[Root.Children.Count - 2]);

                if (IsArrangeValid) target.Header.Background = brush;
                else target.Resources["TitleBrush"] = brush;
            }

            if (IsArrangeValid) Header.Background = brush;
            else this.Resources["TitleBrush"] = brush;
        }

        public void Select() {
            ApplyHeaderBrush("ThemeAccentBrush2");
            selected = true;
        }

        public void Deselect() {
            ApplyHeaderBrush("ThemeControlLowBrush");
            selected = false;
        }

        public DeviceViewer() => InitializeComponent();

        public DeviceViewer(Device device) {
            InitializeComponent();

            TitleText.Text = device.Name;

            _device = device;
            _device.Viewer = this;
            Deselect();

            DeviceContextMenu = (ContextMenu)this.Resources["DeviceContextMenu"];
            GroupContextMenu = (ContextMenu)this.Resources["GroupContextMenu"];
            
            DeviceContextMenu.AddHandler(MenuItem.ClickEvent, ContextMenu_Click);
            GroupContextMenu.AddHandler(MenuItem.ClickEvent, ContextMenu_Click);
            
            this.AddHandler(DragDrop.DropEvent, Drop);
            this.AddHandler(DragDrop.DragOverEvent, DragOver);

            SpecificViewer = GetSpecificViewer(this, _device);

            if (SpecificViewer != null)
                this.Get<Grid>("Contents").Children.Add(SpecificViewer);
            
            SetEnabled();
        }

        protected virtual void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            ResetEvents();

            SpecificViewer = null;
            _device.Viewer = null;
            _device = null;

            DeviceContextMenu.RemoveHandler(MenuItem.ClickEvent, ContextMenu_Click);
            GroupContextMenu.RemoveHandler(MenuItem.ClickEvent, ContextMenu_Click);
            DeviceContextMenu = GroupContextMenu = null;
            
            this.RemoveHandler(DragDrop.DropEvent, Drop);
            this.RemoveHandler(DragDrop.DragOverEvent, DragOver);
        }

        public virtual void SetEnabled() {
            Border.Background = (IBrush)Application.Current.Styles.FindResource(_device.Enabled? "ThemeControlHighBrush" : "ThemeControlMidBrush");
            Border.BorderBrush = (IBrush)Application.Current.Styles.FindResource(_device.Enabled? "ThemeBorderMidBrush" : "ThemeBorderLowBrush");
            TitleText.Foreground = (IBrush)Application.Current.Styles.FindResource(_device.Enabled? "ThemeForegroundBrush" : "ThemeForegroundLowBrush");

            if (Root.Children[0].GetType() == typeof(DeviceHead))
                ((DeviceHead)Root.Children[0]).SetEnabled(_device.Enabled);

            if (Root.Children[Root.Children.Count - 2].GetType() == typeof(DeviceTail))
                ((DeviceTail)Root.Children[Root.Children.Count - 2]).SetEnabled(_device.Enabled);
        }

        protected void Device_Add(Type device) => Added?.Invoke(_device.ParentIndex.Value + 1, device);

        protected void Device_Action(string action) => Track.Get(_device)?.Window?.Selection.Action(action, _device.Parent, _device.ParentIndex.Value);

        protected void ContextMenu_Click(object sender, EventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
                Track.Get(_device)?.Window?.Selection.Action((string)((MenuItem)item).Header);
        }

        void Select(PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed || (MouseButton == PointerUpdateKind.RightButtonPressed && !selected))
                Track.Get(_device)?.Window?.Selection.Select(_device, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
        }

        public async void Drag(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (!selected) Select(e);

            DataObject dragData = new DataObject();
            dragData.Set("device", Track.Get(_device)?.Window?.Selection.Selection);

            App.Dragging = true;
            DragDropEffects result = await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
            App.Dragging = false;

            if (result == DragDropEffects.None) {
                if (selected) Select(e);
                
                if (MouseButton == PointerUpdateKind.RightButtonPressed) {
                    ContextMenu menu = DeviceContextMenu;
                    List<ISelect> selection = Track.Get(_device)?.Window?.Selection.Selection;

                    if (selection.Count == 1 && selection[0].GetType() == typeof(Group) && ((Group)selection[0]).Count == 1)
                        menu = GroupContextMenu;
                    
                    DeviceMute.Header = GroupMute.Header = ((Device)selection.First()).Enabled? "Mute" : "Unmute";

                    menu.Open(Draggable);
                
                } else if (MouseButton == PointerUpdateKind.LeftButtonPressed && e.ClickCount == 2) {
                    _device.Collapsed = !_device.Collapsed;
                    DeviceCollapsed?.Invoke(_device.ParentIndex.Value);
                }
            }
        }

        public void DragOver(object sender, DragEventArgs e) {
            e.Handled = true;
            if (!e.Data.Contains("device") && !e.Data.Contains(DataFormats.FileNames)) e.DragEffects = DragDropEffects.None; 
        }

        public void Drop(object sender, DragEventArgs e) {
            e.Handled = true;

            IControl source = (IControl)e.Source;
            while (source.Name != "DropZoneHead" && source.Name != "Contents" && source.Name != "DropZoneTail" && source.Name != "DropZoneAfter") {
                source = source.Parent;
                
                if (source == this) {
                    e.Handled = false;
                    return;
                }
            }

            Chain _chain = _device.Parent;

            int after = _device.ParentIndex.Value;
            if (source.Name == "DropZoneHead" || (source.Name == "Contents" && e.GetPosition(source).X < source.Bounds.Width / 2)) after--;

            if (e.Data.Contains(DataFormats.FileNames)) {
                string path = e.Data.GetFileNames().FirstOrDefault();

                if (path != null) _chain.Viewer?.Import(after, path);

                return;
            }

            if (!e.Data.Contains("device")) return;

            List<Device> moving = ((List<ISelect>)e.Data.Get("device")).Select(i => (Device)i).ToList();
            Chain source_parent = moving[0].Parent;
            int before = moving[0].IParentIndex.Value - 1;

            bool copy = e.Modifiers.HasFlag(App.ControlInput);

            bool result = Device.Move(moving, _chain, after, copy);

            if (result) {
                int before_pos = before;
                int after_pos = moving[0].IParentIndex.Value - 1;
                int count = moving.Count;

                if (source_parent == _chain && after < before)
                    before_pos += count;
                
                List<int> sourcepath = Track.GetPath(source_parent);
                List<int> targetpath = Track.GetPath(_chain);
                
                Program.Project.Undo.Add($"Device {(copy? "Copied" : "Moved")}", copy
                    ? new Action(() => {
                        Chain targetchain = ((Chain)Track.TraversePath(targetpath));

                        for (int i = after + count; i > after; i--)
                            targetchain.Remove(i);

                    }) : new Action(() => {
                        Chain sourcechain = ((Chain)Track.TraversePath(sourcepath));
                        Chain targetchain = ((Chain)Track.TraversePath(targetpath));

                        List<Device> umoving = (from i in Enumerable.Range(after_pos + 1, count) select targetchain[i]).ToList();

                        Device.Move(umoving, sourcechain, before_pos);

                }), () => {
                    Chain sourcechain = ((Chain)Track.TraversePath(sourcepath));
                    Chain targetchain = ((Chain)Track.TraversePath(targetpath));

                    List<Device> rmoving = (from i in Enumerable.Range(before + 1, count) select sourcechain[i]).ToList();

                    Device.Move(rmoving, targetchain, after, copy);
                });
            
            } else e.DragEffects = DragDropEffects.None;
        }
    }
}
