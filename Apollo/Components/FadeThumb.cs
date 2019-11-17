using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

using System;
using System.Linq;


using Apollo.Enums;
using System.Collections.Generic;

namespace Apollo.Components {
    public class FadeThumb: UserControl {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Base = this.Get<Thumb>("Thumb");
        }

        public delegate void MovedEventHandler(FadeThumb sender, double change, double? total);
        public event MovedEventHandler Moved;

        public delegate void FadeThumbEventHandler(FadeThumb sender);
        public event FadeThumbEventHandler Focused;
        public event FadeThumbEventHandler Deleted;
        public event FadeThumbEventHandler MenuOpened;
        public event FadeThumbEventHandler FadeTypeChanged;
        public FadeType Type = FadeType.Linear;
        ContextMenu ThumbContextMenu;
        List<MenuItem> MenuItems;
        Separator DeleteSeparator;
        public Thumb Base;
        
        public void RemoveDelete(){
            MenuItems.Last().IsVisible = false;
            DeleteSeparator.IsVisible = false;
        }

        public IBrush Fill {
            get => (IBrush)this.Resources["Color"];
            set => this.Resources["Color"] = value;
        }

        public FadeThumb() {
            InitializeComponent();

            ThumbContextMenu = (ContextMenu)this.Resources["ThumbContextMenu"];
            MenuItems = ThumbContextMenu.Items.OfType<MenuItem>().ToList();
            
            DeleteSeparator = this.Get<Separator>("DeleteSeparator");

            ThumbContextMenu.AddHandler(MenuItem.ClickEvent, ContextMenu_Click);

            Base.AddHandler(InputElement.PointerPressedEvent, MouseDown, RoutingStrategies.Tunnel);
            Base.AddHandler(InputElement.PointerReleasedEvent, MouseUp, RoutingStrategies.Tunnel);

        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            Moved = null;
            Focused = null;
            Deleted = null;

            Base.RemoveHandler(InputElement.PointerPressedEvent, MouseDown);
            Base.RemoveHandler(InputElement.PointerReleasedEvent, MouseUp);
        }

        bool dragged = false;

        void DragStarted(object sender, VectorEventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            dragged = false;
        }

        void DragCompleted(object sender, VectorEventArgs e) {
            if (!dragged) Focused?.Invoke(this);
            else if (change != 0) Moved?.Invoke(this, 0, change);
        }

        void MouseDown(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton != PointerUpdateKind.LeftButtonPressed) e.Handled = true;

            ((Window)this.GetVisualRoot()).Focus();
        }

        void MouseUp(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.RightButtonReleased) {
                MenuItems.ForEach(i => i.Icon = "");
                MenuItems[(int)Type].Icon = this.Resources["SelectedIcon"];
                System.Console.WriteLine((int)Type);
                MenuOpened?.Invoke(this);
                e.Handled = true;
            }
        }

        double change;

        void MouseMove(object sender, VectorEventArgs e) {
            if (!dragged) change = 0;
            change += e.Vector.X;

            dragged = true;
            Moved?.Invoke(this, e.Vector.X, null);
        }

        public void Select() => this.Resources["Outline"] = new SolidColorBrush(new Color(255, 255, 255, 255));
        public void Unselect() => this.Resources["Outline"] = new SolidColorBrush(new Color(0, 255, 255, 255));

        public void OpenMenu() {
            ThumbContextMenu.Open(Base);
        }
        public void ContextMenu_Click(object sender, EventArgs e) {
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item is MenuItem selectedItem ) {
                string header = selectedItem.Header.ToString();
                Console.WriteLine(header);

                if (header == "Delete") {
                    Deleted?.Invoke(this);
                }
                else {
                    Enum.TryParse(header, out Type);
                    FadeTypeChanged?.Invoke(this);
                }
            }
        }
    }
}
