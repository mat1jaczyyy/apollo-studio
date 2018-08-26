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

        public static Chain _chain;

        // Initialize Program
        static void Main(string[] args) {
            _chain = new Chain(MIDIExit);
            _chain.Add(
                new Group(new Chain[] {
                    new Chain(new Device[] {
                        new Pitch(19),
                        new Chord(12)
                    }),
                    new Chain(new Device[] {
                        new Chord(12)
                    })
                })
            );

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

            _chain.MIDIEnter(n);
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
