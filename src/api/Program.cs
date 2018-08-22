using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Enums;
using RtMidi.Core.Messages;

using api.Devices;

namespace api {
    public class Communication {
        public struct Note {
            public int p;
            public int r;
            public int g;
            public int b;
        }
    }

    class Program {
        // MIDI Access
        static IMidiInputDevice iDevice;
        static IMidiOutputDevice oDevice;

        // Chain of the Lights track
        static List<Devices.Device> _chain = new List<Devices.Device>();

        // Access chain
        public static List<Devices.Device> Chain {
            get {
                return _chain;
            }
        }

        // Initialize Program
        static void Main(string[] args) {
            _chain.Add(new Devices.Pitch(3, MIDIExit));
            _chain.Add(new Devices.Chord(10, MIDIExit));
            _chain[0].MIDIExit = _chain[1].MIDIEnter;

            foreach (var api in MidiDeviceManager.Default.GetAvailableMidiApis())
                Console.WriteLine($"API: {api}");
            
            iDevice = MidiDeviceManager.Default.InputDevices.Last().CreateDevice();
            Console.WriteLine($"Input: {iDevice.Name}");
            iDevice.NoteOn += NoteOn;
            iDevice.Open();

            oDevice = MidiDeviceManager.Default.OutputDevices.Last().CreateDevice();
            Console.WriteLine($"Output: {oDevice.Name}");
            oDevice.Open();

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();
            
            host.Run(); // Halts the thread!
        }

        static void MIDIExit(Communication.Note n) {
            NoteOnMessage msg = new NoteOnMessage(RtMidi.Core.Enums.Channel.Channel1, (RtMidi.Core.Enums.Key)(n.p), n.r << 1);
            Console.WriteLine($"OUT <- {msg.Key.ToString()} {msg.Velocity.ToString()}");

            oDevice.Send(in msg);
        }

        static void NoteOn(object sender, in NoteOnMessage e) {
            Console.WriteLine($"IN  -> {e.Key.ToString()} {e.Velocity.ToString()}");

            Communication.Note n = new Communication.Note();
            n.p = (int)(e.Key);
            n.r = e.Velocity >> 1;
            n.g = e.Velocity >> 1;
            n.g = e.Velocity >> 1;

            _chain[0].MIDIEnter(n);
        }
    }

    public class Startup {
        public Startup(IHostingEnvironment env) {}

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            // Add framework services.
            services.AddMvc().AddJsonOptions(options => {
                //return json format with Camel Case
                options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app) {
            app.UseMvc();
        }
    }
}
