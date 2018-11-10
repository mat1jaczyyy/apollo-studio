using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

using api;

namespace api.Devices {
    public class Delay: Device {
        private int _length = 200; // milliseconds
        private Decimal _gate = 1;
        private Queue<Timer> _timers = new Queue<Timer>();
        private TimerCallback _timerexit;

        public int Length {
            get {
                return _length;
            }
            set {
                if (0 <= value)
                    _length = value;
            }
        }

        public Decimal Gate {
            get {
                return _gate;
            }
            set {
                if (0 <= value && value <= 4)
                    _gate = value;
            }
        }

        public override Device Clone() {
            return new Delay(_length, _gate);
        }

        public Delay() {
            _timerexit = new TimerCallback(Tick);
        }

        public Delay(int length, Decimal gate) {
            _timerexit = new TimerCallback(Tick);
            Length = length;
            Gate = gate;
        }

        public Delay(Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            MIDIExit = exit;
        }

        public Delay(int length, Decimal gate, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            Length = length;
            Gate = gate;
            MIDIExit = exit;
        }

        private void Tick(object info) {
            if (info.GetType() == typeof(Signal)) {
                Signal n = (Signal)info;
      
                if (MIDIExit != null)
                    MIDIExit(n);
                
                try {
                    _timers.Dequeue();
                } catch {
                    if (api.Program.log)
                        Console.WriteLine($"ERR ** Delay Dequeue skipped");
                } 
            }
        }

        public override void MIDIEnter(Signal n) {
            try {
                _timers.Enqueue(new Timer(_timerexit, n.Clone(), Convert.ToInt32(_length * _gate), System.Threading.Timeout.Infinite));
            } catch {
                if (api.Program.log)
                    Console.WriteLine($"ERR ** Delay Enqueue skipped");
            } 
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != "delay") return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());

            return new Delay(int.Parse(data["length"].ToString()), int.Parse(data["gate"].ToString()));
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue("delay");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("length");
                        writer.WriteValue(_length);

                        writer.WritePropertyName("gate");
                        writer.WriteValue(_gate);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}