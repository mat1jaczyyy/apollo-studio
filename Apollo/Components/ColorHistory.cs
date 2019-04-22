using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Input;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Components {
    public class ColorHistory: UserControl {
        public static readonly string Identifier = "colorhistory";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void HistoryChangedEventHandler();
        public static event HistoryChangedEventHandler HistoryChanged;

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

            HistoryChanged?.Invoke();
        }
        
        static ColorHistory() => HistoryChanged += Preferences.Save;

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

        public void Use() {
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

        private void Draw() {
            int offset = 0;

            if (CurrentIndex == -1) {
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
                    box.StrokeThickness = Convert.ToInt32(Current == History[i] && CurrentIndex != -1);

                } else box.Opacity = 0;
            }
        }

        public void Render(Launchpad launchpad) {
            launchpad?.Clear();
            int offset = 0;

            if (CurrentIndex == -1) {
                offset = 1;
                launchpad?.Send(new Signal(launchpad, 81, Current));
            }

            int x = 8;
            int y = 1 + offset;

            for (int i = 0; i < 64 - offset; i++) {
                if (i < History.Count) {
                    launchpad?.Send(new Signal(launchpad, (byte)(x * 10 + (y++)), History[i]));

                    if (y > 8) {
                        x--;
                        y = 1;
                    }

                } else break;
            }
        }

        public ColorHistory() {
            InitializeComponent();

            Grid = this.Get<UniformGrid>("Grid");

            HistoryChanged += Draw;
            Draw();
        }

        private void Clicked(object sender, PointerReleasedEventArgs e) => Input(Grid.Children.IndexOf((IControl)sender));

        public static void Decode(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != Identifier) return;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            List<object> history = JsonConvert.DeserializeObject<List<object>>(data["history"].ToString());

            History = (from i in history select Color.Decode(i.ToString())).ToList();
        }

        public static string Encode() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("object");
                    writer.WriteValue(Identifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("history");
                        writer.WriteStartArray();

                            for (int i = 0; i < 64; i++)
                                if (i < History.Count)
                                    writer.WriteRawValue(History[i].Encode());
                                
                        writer.WriteEndArray();

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}
