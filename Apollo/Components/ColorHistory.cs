using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Input;

using Apollo.Devices;
using Apollo.Structures;

namespace Apollo.Components {
    public class ColorHistory: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private static List<Color> History = new List<Color>();

        public Color this[int index] {
            get => (index < Count)? History[index] : null;
        }

        public int Count {
            get => History.Count;
        }

        public static void Use(Color color) {
            if (History.Contains(color)) History.Remove(color);
            History.Insert(0, color);
        }

        public delegate void ColorChangedEventHandler(Color value);
        public event ColorChangedEventHandler ColorChanged;

        Color _current;
        Color Current {
            get => _current;
            set {
                _current = value;
                ColorChanged?.Invoke(_current);
            }
        }
        
        int CurrentIndex;
        bool Saved = true;

        public void Use() {
            Use(Current);

            CurrentIndex = 0;
            Saved = true;

            Draw();
        }

        public void Select(Color color) {
            _current = color;
            CurrentIndex = -1;
            Saved = false;

            Draw();
        }

        public void Select(int index) {
            if (index == -1) {
                Use();
                return;
            }

            Current = History[index];
            CurrentIndex = index;
            Saved = true;

            Draw();
        }
        
        UniformGrid Grid;

        private void Draw() {
            int offset = 0;

            if (!Saved) {
                offset = 1;

                Rectangle box = ((Rectangle)Grid.Children[0]);
                box.Opacity = 1;
                box.Fill = Current.ToBrush();
                box.StrokeThickness = 1;
            }

            for (int i = 0; i < 64 - offset; i++) {
                Rectangle box = ((Rectangle)Grid.Children[i + offset]);

                if (i < History.Count) {
                    box.Opacity = 1;
                    box.Fill = History[i].ToBrush();
                    box.StrokeThickness = Convert.ToInt32(i == CurrentIndex);

                } else box.Opacity = 0;
            }
        }

        public ColorHistory() {
            InitializeComponent();

            Grid = this.Get<UniformGrid>("Grid");

            Draw();
        }

        private void Clicked(object sender, PointerReleasedEventArgs e) {
            int index = Grid.Children.IndexOf((IControl)sender);
            Select((CurrentIndex == -1)? (index - 1) : index);
        } 
    }
}
