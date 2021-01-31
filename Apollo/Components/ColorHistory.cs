using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Input;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Rendering;
using Apollo.Structures;

namespace Apollo.Components {
    public class ColorHistory: UserControl {
        public delegate void HistoryChangedEventHandler();
        public static event HistoryChangedEventHandler HistoryChanged;

        static List<Color> History = new();

        public static Color GetColor(int index) => (index < Count)? History[index] : null;

        public static void Set(List<Color> history) {
            History = history;
            HistoryChanged?.Invoke();
        }

        public static int Count => History.Count;

        static void Use(Color color) {
            if (History.Contains(color)) History.Remove(color);
            History.Insert(0, color);

            HistoryChanged?.Invoke();
        }

        public static void Clear() {
            History.Clear();
            HistoryChanged?.Invoke();
        }
        
        static ColorHistory() => HistoryChanged += Preferences.Save;

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Grid = this.Get<UniformGrid>("Grid");
        }

        public delegate void ColorChangedEventHandler(Color value);
        public event ColorChangedEventHandler ColorChanged;

        Color _current = new Color();
        Color Current {
            get => _current;
            set {
                _current = value;
                ColorChanged?.Invoke(_current);
            }
        }
        
        UniformGrid Grid;
        int CurrentIndex;

        public int AdjustedCount => History.Count + Convert.ToInt32(CurrentIndex == -1);

        public void Use() {
            if (Current == new Color(0)) return;

            Use(Current);

            CurrentIndex = 0;

            Draw();
        }

        public void Select(Color color, bool init = false) {
            _current = color;
            CurrentIndex = init? History.IndexOf(color) : -1;

            Draw();
        }

        public void Select(int index) {
            if (index == -1) {
                Use();
                return;
            }

            Current = History[index];
            CurrentIndex = index;

            Draw();
        }

        public void Input(int index) => Select((CurrentIndex == -1)? (index - 1) : index);

        void Draw() {
            int offset = 0;

            if (CurrentIndex == -1) {
                offset = 1;

                Rectangle box = ((Rectangle)Grid.Children[0]);
                box.Opacity = 1;
                box.IsHitTestVisible = true;
                box.Fill = Current.ToBrush();
                box.StrokeThickness = 1;
            }

            for (int i = 0; i < 64 - offset; i++) {
                Rectangle box = ((Rectangle)Grid.Children[i + offset]);

                if (i < History.Count) {
                    box.Opacity = 1;
                    box.IsHitTestVisible = true;
                    box.Fill = History[i].ToBrush();
                    box.StrokeThickness = Convert.ToInt32(Current == History[i] && CurrentIndex != -1);

                } else {
                    box.Opacity = 0;
                    box.IsHitTestVisible = false;
                }
            }
        }

        public void Render(Launchpad launchpad) {
            launchpad?.Clear();
            int offset = 0;

            if (CurrentIndex == -1) {
                offset = 1;
                launchpad?.DirectSend(new RawUpdate(81, Current));
            }

            int x = 8;
            int y = 1 + offset;

            for (int i = 0; i < 64 - offset; i++) {
                if (i < History.Count) {
                    launchpad?.DirectSend(new RawUpdate((byte)(x * 10 + y++), History[i]));

                    if (y > 8) {
                        x--;
                        y = 1;
                    }

                } else break;
            }
        }

        public ColorHistory() {
            InitializeComponent();

            HistoryChanged += Draw;
            Draw();
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            ColorChanged = null;
            HistoryChanged -= Draw;
        }

        void Clicked(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            int index = Grid.Children.IndexOf((IControl)sender);

            if (MouseButton == PointerUpdateKind.LeftButtonReleased) Input(index);
            else if (MouseButton == PointerUpdateKind.RightButtonReleased) {
                if ((index = (CurrentIndex == -1)? (index - 1) : index) == -1) return;
                
                History.RemoveAt(index);
                HistoryChanged?.Invoke();
                
                Select(CurrentIndex - Convert.ToInt32(index <= CurrentIndex));
            }
        }
    }
}
