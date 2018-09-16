using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

using api;

namespace api.Devices {
    public class Iris: Device {
        private int _rate = 200; // milliseconds
        private List<Color> _colors = new List<Color>();
        private Timer[] _timers = new Timer[128];
        private TimerCallback _timerexit;

        public int Rate {
            get {
                return _rate;
            }
            set {
                if (0 <= value)
                    _rate = value;
            }
        }

        public override Device Clone() {
            return new Iris(_rate);
        }

        public Iris() {
            _timerexit = new TimerCallback(Tick);
        }

        public Iris(int rate) {
            _timerexit = new TimerCallback(Tick);
            Rate = rate;
        }

        public Iris(Color[] colors) {
            _timerexit = new TimerCallback(Tick);
            _colors = colors.ToList();
        }

        public Iris(List<Color> colors) {
            _timerexit = new TimerCallback(Tick);
            _colors = colors;
        }

        public Iris(int rate, Color[] colors) {
            _timerexit = new TimerCallback(Tick);
            Rate = rate;
            _colors = colors.ToList();
        }

        public Iris(int rate, List<Color> colors) {
            _timerexit = new TimerCallback(Tick);
            Rate = rate;
            _colors = colors;
        }

        public Iris(Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            MIDIExit = exit;
        }

        public Iris(int rate, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            Rate = rate;
            MIDIExit = exit;
        }

        public Iris(Color[] colors, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            _colors = colors.ToList();
            MIDIExit = exit;
        }

        public Iris(List<Color> colors, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            _colors = colors;
            MIDIExit = exit;
        }

        public Iris(int rate, Color[] colors, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            Rate = rate;
            _colors = colors.ToList();
            MIDIExit = exit;
        }

        public Iris(int rate, List<Color> colors, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            Rate = rate;
            _colors = colors;
            MIDIExit = exit;
        }

        private void Tick(object info) {
            if (info.GetType() == typeof((byte, int))) {
                (byte index, int i) = ((byte, int))info;
                if (++i < _colors.Count) {
                    _timers[index] = new Timer(_timerexit, (index, i), _rate, System.Threading.Timeout.Infinite);
                    
                    Signal n = new Signal(index, _colors[i].Clone());

                    if (MIDIExit != null)
                        MIDIExit(n);
                
                } else {
                    Signal n = new Signal(index, new Color(0));

                    if (MIDIExit != null)
                        MIDIExit(n);
                }
            }
        }

        public override void MIDIEnter(Signal n) {
            if (_colors.Count > 0)
                if (n.Color.Lit) {
                    if (_timers[n.Index] != null)
                        _timers[n.Index].Dispose();

                    _timers[n.Index] = new Timer(_timerexit, (n.Index, 0), _rate, System.Threading.Timeout.Infinite);

                    n.Color = _colors[0].Clone();

                    if (MIDIExit != null)
                        MIDIExit(n);
                }
        }

         public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue("iris");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("rate");
                        writer.WriteValue(_rate);

                        writer.WritePropertyName("colors");
                        writer.WriteStartObject();

                            writer.WritePropertyName("count");
                            writer.WriteValue(_colors.Count);

                            for (int i = 0; i < _colors.Count; i++) {
                                writer.WritePropertyName(i.ToString());
                                writer.WriteRawValue(_colors[i].Encode());
                            }

                        writer.WriteEndObject();

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}