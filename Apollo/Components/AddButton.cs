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
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
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

        protected void MouseEnter(object sender, PointerEventArgs e) {
            Fill = (IBrush)Application.Current.Styles.FindResource(mouseHeld? "ThemeButtonDownBrush" : "ThemeButtonOverBrush");
            
            if (sender is Grid grid && Preferences.UIMotion != UIMotionType.Off) {
                bool isHorizontal = grid.ColumnDefinitions?.Count > 0;
                    
                int hoverTime = Preferences.UIHoverTime switch
                {
                    UIHoverTimeType.Instant => 1,
                    UIHoverTimeType.Fast => 100,
                    UIHoverTimeType.Normal => 250,
                    UIHoverTimeType.Slow => 500,
                    _ => 1
                };
                
                if (Preferences.UIMotion == UIMotionType.Full) {
                    new Animation {
                        Duration = Preferences.UIMotion == UIMotionType.Reduced? TimeSpan.FromMilliseconds(1) : TimeSpan.FromMilliseconds(150),
                        Easing = new QuadraticEaseInOut(),
                        Delay = TimeSpan.FromMilliseconds(hoverTime),
                        Children = {
                            new KeyFrame {
                                Setters =
                                {
                                    new Setter(isHorizontal ? WidthProperty : HeightProperty, isHorizontal ? 6 : 5)
                                },
                                Cue = new Cue(0)
                            },
                            new KeyFrame {
                                Setters =
                                {
                                    new Setter(isHorizontal ? WidthProperty : HeightProperty, isHorizontal ? 30 : 26),
                                },
                                Cue = new Cue(1)
                            }
                        }
                    }.RunAsync(grid); //TODO: Make this actually run
                }
            }
        }

        protected void MouseLeave(object sender, PointerEventArgs e) {
            Fill = (IBrush)Application.Current.Styles.FindResource("ThemeButtonEnabledBrush");
            mouseHeld = false;
            
            if (sender is Grid grid && Preferences.UIMotion != UIMotionType.Off) {
                bool isHorizontal = grid.ColumnDefinitions?.Count > 0;
                new Animation {
                    Duration = Preferences.UIMotion == UIMotionType.Reduced? TimeSpan.FromMilliseconds(1) : TimeSpan.FromMilliseconds(100),
                    Easing = new QuadraticEaseInOut(),
                    Children = {
                        new KeyFrame {
                            Setters = { new Setter(isHorizontal ? WidthProperty : HeightProperty, isHorizontal ? grid.Width : grid.Height) }, 
                            Cue = new Cue(0)
                        },
                        new KeyFrame {
                            Setters = { new Setter(isHorizontal ? WidthProperty : HeightProperty, isHorizontal ? 6 : 5) }, 
                            Cue = new Cue(1)
                        }
                    }
                }.RunAsync(grid); //TODO: Make this actually run
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
