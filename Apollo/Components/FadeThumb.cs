using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

using System;

using Apollo.Enums;

namespace Apollo.Components
{
    public class FadeThumb : UserControl
    {
        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            Base = this.Get<Thumb>("Thumb");
            Delete = this.Get<MenuItem>("Delete");
        }

        public delegate void MovedEventHandler(FadeThumb sender, double change, double? total);
        public event MovedEventHandler Moved;

        public delegate void FadeThumbEventHandler(FadeThumb sender);
        public event FadeThumbEventHandler Focused;
        public event FadeThumbEventHandler Deleted;
        public event FadeThumbEventHandler MenuOpened;
        public event FadeThumbEventHandler FadeTypeChanged;
        public FadeTypes FadeType = FadeTypes.Linear;
        ContextMenu ThumbContextMenu;
        public Thumb Base;
        public MenuItem Delete;

        public IBrush Fill
        {
            get => (IBrush)this.Resources["Color"];
            set => this.Resources["Color"] = value;
        }

        public FadeThumb()
        {
            InitializeComponent();

            ThumbContextMenu = (ContextMenu)this.Resources["ThumbContextMenu"];

            ThumbContextMenu.AddHandler(MenuItem.ClickEvent, ContextMenu_Click);

            Base.AddHandler(InputElement.PointerPressedEvent, MouseDown, RoutingStrategies.Tunnel);
            Base.AddHandler(InputElement.PointerReleasedEvent, MouseUp, RoutingStrategies.Tunnel);

        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e)
        {
            Moved = null;
            Focused = null;
            Deleted = null;

            Base.RemoveHandler(InputElement.PointerPressedEvent, MouseDown);
            Base.RemoveHandler(InputElement.PointerReleasedEvent, MouseUp);
        }

        bool dragged = false;

        void DragStarted(object sender, VectorEventArgs e)
        {
            ((Window)this.GetVisualRoot()).Focus();
            dragged = false;
        }

        void DragCompleted(object sender, VectorEventArgs e)
        {
            if (!dragged) Focused?.Invoke(this);
            else if (change != 0) Moved?.Invoke(this, 0, change);
        }

        void MouseDown(object sender, PointerPressedEventArgs e)
        {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton != PointerUpdateKind.LeftButtonPressed) e.Handled = true;

            ((Window)this.GetVisualRoot()).Focus();
        }

        void MouseUp(object sender, PointerReleasedEventArgs e)
        {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.RightButtonReleased)
            {
                MenuItem selectedItem = null;
                foreach (Control item in ThumbContextMenu.Items)
                {
                    if (item.GetType() != typeof(MenuItem)) break;
                    MenuItem menuItem = (MenuItem)item;
                    menuItem.Icon = "";
                    if (menuItem.Header.ToString() == FadeType.ToString())
                    {
                        selectedItem = menuItem;
                    }
                }
                selectedItem.Icon = this.Resources["SelectedIcon"];
                MenuOpened?.Invoke(this);
                e.Handled = true;
            }
        }

        double change;

        void MouseMove(object sender, VectorEventArgs e)
        {
            if (!dragged) change = 0;
            change += e.Vector.X;

            dragged = true;
            Moved?.Invoke(this, e.Vector.X, null);
        }

        public void Select() => this.Resources["Outline"] = new SolidColorBrush(new Color(255, 255, 255, 255));
        public void Unselect() => this.Resources["Outline"] = new SolidColorBrush(new Color(0, 255, 255, 255));

        public void OpenMenu()
        {
            ThumbContextMenu.Open(Base);
        }
        public void ContextMenu_Click(object sender, EventArgs e)
        {
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item.GetType() == typeof(MenuItem))
            {
                MenuItem selectedItem = (MenuItem)item;
                string header = selectedItem.Header.ToString();

                if (header == "Delete")
                {
                    Deleted?.Invoke(this);
                }
                else
                {
                    Enum.TryParse(header, out FadeType);
                    FadeTypeChanged?.Invoke(this);
                }
            }
        }
    }
}
