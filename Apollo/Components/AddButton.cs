using System;
using System.Diagnostics;
using Apollo.Core;
using Apollo.Enums;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace Apollo.Components {
    public abstract class AddButton: UserControl {
        public delegate void AddedEventHandler();
        public event AddedEventHandler Added;

        protected void InvokeAdded() => Added?.Invoke();

        protected Path Path;

        protected IBrush Fill {
            get => Path.Stroke;
            set => Path.Stroke = value;
        }

        protected bool AllowRightClick = false;

        protected Grid Root;

        protected bool _always;
        public virtual bool AlwaysShowing {
            get => _always;
            set {}
        }

        protected virtual void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => Added = null;

        bool mouseHeld = false;
        
        private DispatcherTimer _hoverTimer;

        protected void MouseEnter(object sender, PointerEventArgs e) {
            Fill = (IBrush)Application.Current.Styles.FindResource(mouseHeld? "ThemeButtonDownBrush" : "ThemeButtonOverBrush");
            
            if (_hoverTimer != null) {
                _hoverTimer.Stop();
            }
            
            if (sender is Grid grid && Preferences.UIMotion != UIMotionType.Off)
            {
                grid.Transitions = null;
                bool isHorizontal = grid.ColumnDefinitions?.Count > 0;

                int hoverTime = Preferences.UIHoverTime switch
                {
                    UIHoverTimeType.None => 0,
                    UIHoverTimeType.Short => 100,
                    UIHoverTimeType.Medium => 250,
                    UIHoverTimeType.Long => 500,
                    _ => 0
                };
                
                _hoverTimer = new DispatcherTimer {
                    Interval = TimeSpan.FromMilliseconds(hoverTime)
                };
        
                _hoverTimer.Tick += (s, args) => {
                    grid.Transitions = null;
                    
                    if (Preferences.UIMotion == UIMotionType.Full) {
                        grid.Transitions = new Transitions { new DoubleTransition {
                            Property = isHorizontal ? WidthProperty : HeightProperty,
                            Duration = TimeSpan.FromSeconds(0.15),
                            Easing = new QuadraticEaseInOut()
                        }};
                    }

                    if (isHorizontal)
                    {
                        grid.Width = 30;
                    }
                    else
                    {
                        grid.Height = 26;
                    }
                    
                    _hoverTimer.Stop();
                    _hoverTimer = null;
                };
        
                _hoverTimer.Start();
            }
        }

        protected void MouseLeave(object sender, PointerEventArgs e) {
            Fill = (IBrush)Application.Current.Styles.FindResource("ThemeButtonEnabledBrush");
            mouseHeld = false;
            
            if (_hoverTimer != null) {
                _hoverTimer.Stop();
                _hoverTimer = null;
            }

            if (sender is Grid grid && Preferences.UIMotion != UIMotionType.Off) {
                bool isHorizontal = grid.ColumnDefinitions?.Count > 0;
                
                grid.Transitions = null;
                if (Preferences.UIMotion == UIMotionType.Full) {
                    grid.Transitions = new Transitions { new DoubleTransition {
                        Property = isHorizontal ? WidthProperty : HeightProperty,
                        Duration = TimeSpan.FromSeconds(0.1),
                        Easing = new QuadraticEaseInOut()
                    }};
                }

                if (isHorizontal)
                {
                    grid.Width = 6;
                }
                else
                {
                    grid.Height = 5;
                }
            }
        }

        protected void MouseDown(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed || (AllowRightClick && MouseButton == PointerUpdateKind.RightButtonPressed)) {
                mouseHeld = true;

                Fill = (IBrush)Application.Current.Styles.FindResource("ThemeButtonDownBrush");
            }
        }

        protected void MouseUp(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (mouseHeld && (MouseButton == PointerUpdateKind.LeftButtonReleased || (AllowRightClick && MouseButton == PointerUpdateKind.RightButtonReleased))) {
                mouseHeld = false;

                MouseEnter(sender, null);

                Click(e);
            }
        }

        protected virtual void Click(PointerReleasedEventArgs e) => Added?.Invoke();
    }
}
