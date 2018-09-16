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

        public override Device Clone() {
            return new Delay(_length);
        }

        public Delay() {
            _timerexit = new TimerCallback(Tick);
        }

        public Delay(int length) {
            _timerexit = new TimerCallback(Tick);
            Length = length;
        }

        public Delay(Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            MIDIExit = exit;
        }

        public Delay(int length, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            Length = length;
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
                _timers.Enqueue(new Timer(_timerexit, n.Clone(), _length, System.Threading.Timeout.Infinite));
            } catch {
                if (api.Program.log)
                    Console.WriteLine($"ERR ** Delay Enqueue skipped");
            } 
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue("delay");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("time");
                        writer.WriteValue(_length);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}