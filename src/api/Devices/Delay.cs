using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using api;

namespace api.Devices {
    public class Delay: Device {
        public bool Mode = false; // true uses Length
        public Length Length = new Length();

        private int _time = 500;
        private Decimal _gate = 1;

        private Queue<Timer> _timers = new Queue<Timer>();
        private TimerCallback _timerexit;

        public int Time {
            get {
                return _time;
            }
            set {
                if (10 <= value && value <= 30000)
                    _time = value;
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
            return new Delay(Mode, Length, _time, _gate);
        }

        public Delay() {
            _timerexit = new TimerCallback(Tick);
        }

        public Delay(int time, Decimal gate) {
            _timerexit = new TimerCallback(Tick);
            Time = time;
            Gate = gate;
        }

        public Delay(Length length, Decimal gate) {
            _timerexit = new TimerCallback(Tick);
            Mode = true;
            Length = length;
            Gate = gate;
        }

        public Delay(bool mode, Length length, int time, Decimal gate) {
            _timerexit = new TimerCallback(Tick);
            Mode = mode;
            Time = time;
            Length = length;
            Gate = gate;
        }

        public Delay(Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            MIDIExit = exit;
        }

        public Delay(int time, Decimal gate, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            Time = time;
            Gate = gate;
            MIDIExit = exit;
        }

        public Delay(Length length, Decimal gate, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            Mode = true;
            Length = length;
            Gate = gate;
            MIDIExit = exit;
        }

        public Delay(bool mode, Length length, int time, Decimal gate, Action<Signal> exit) {
            _timerexit = new TimerCallback(Tick);
            Mode = mode;
            Time = time;
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
                _timers.Enqueue(new Timer(_timerexit, n.Clone(), Convert.ToInt32((Mode? (int)Length : _time) * _gate), System.Threading.Timeout.Infinite));
            } catch {
                if (api.Program.log)
                    Console.WriteLine($"ERR ** Delay Enqueue skipped");
            } 
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != "delay") return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());

            return new Delay(Convert.ToBoolean(data["mode"]), new Length(Convert.ToDecimal(data["length"])), Convert.ToInt32(data["time"]), Convert.ToInt32(data["gate"]));
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue("delay");

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("mode");
                        writer.WriteValue(Mode);

                        writer.WritePropertyName("length");
                        writer.WriteValue(Length.Value);

                        writer.WritePropertyName("time");
                        writer.WriteValue(_time);

                        writer.WritePropertyName("gate");
                        writer.WriteValue(_gate);

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }

        public override ObjectResult RequestSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != "message") return new BadRequestObjectResult("Not a message.");
            if (json["recipient"].ToString() != "device") return new BadRequestObjectResult("Incorrect recipient for message.");
            if (json["device"].ToString() != "delay") return new BadRequestObjectResult("Incorrect device recipient for message.");

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());

            switch (data["type"].ToString()) {
                case "forward":
                    return new BadRequestObjectResult("The Delay object has no members to forward to.");
                
                case "mode":
                    Mode = Convert.ToBoolean(data["value"]);
                    return new OkObjectResult(EncodeSpecific());

                case "length":
                    Length = new Length(Convert.ToInt32(data["value"]) + 7);
                    return new OkObjectResult(EncodeSpecific());

                case "time":
                    Time = Convert.ToInt32(data["value"]);
                    return new OkObjectResult(EncodeSpecific());

                case "gate":
                    Gate = Convert.ToDecimal(data["value"]);
                    return new OkObjectResult(EncodeSpecific());
                
                default:
                    return new BadRequestObjectResult("Unknown message type.");
            }
        }
    }
}