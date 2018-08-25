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
    public struct Signal {
        public byte p;
        public byte r;
        public byte g;
        public byte b;
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

        static void MIDIExit(Signal n) {
            byte[] data = {0x00, 0x20, 0x29, 0x02, 0x18, 0x0B, n.p, n.r, n.g, n.b};
            SysExMessage msg = new SysExMessage(data);
            Console.WriteLine($"OUT <- {msg.ToString()}");

            oDevice.Send(in msg);
        }

        static void NoteOn(object sender, in NoteOnMessage e) {
            Console.WriteLine($"IN  -> {e.Key.ToString()} {e.Velocity.ToString()}");

            Signal n = new Signal();
            n.p = (byte)(e.Key);
            n.r = (byte)(e.Velocity >> 1);
            n.g = (byte)(e.Velocity >> 1);
            n.b = (byte)(e.Velocity >> 1);

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
