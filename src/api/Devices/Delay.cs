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
        // TODO: Step-based delays (musical notation)
        private int _length = 500; // milliseconds
        private Decimal _gate = 1;
        private Queue<Timer> _timers = new Queue<Timer>();
        private TimerCallback _timerexit;

        public int Length {
            get {
                return _length;
            }
            set {
                if (10 <= value && value <= 30000)
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

            return new Delay(Convert.ToInt32(data["length"]), Convert.ToInt32(data["gate"]));
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

        public override ObjectResult RequestSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["object"].ToString() != "message") return new BadRequestObjectResult("Not a message.");
            if (json["recipient"].ToString() != "device") return new BadRequestObjectResult("Incorrect recipient for message.");
            if (json["device"].ToString() != "delay") return new BadRequestObjectResult("Incorrect device recipient for message.");

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());

            switch (data["type"].ToString()) {
                case "forward":
                    return new BadRequestObjectResult("The Delay object has no members to forward to.");
                
                case "length":
                    Length = Convert.ToInt32(data["value"]);
                    return new OkObjectResult(null);

                case "gate":
                    Gate = Convert.ToDecimal(data["value"]);
                    return new OkObjectResult(null);
                
                default:
                    return new BadRequestObjectResult("Unknown message type.");
            }
        }
    }
}