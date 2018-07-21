﻿using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Messages;
using api.Devices;

namespace api {
    class Program {
        // MIDI Access
        static List<IMidiInputDevice> devices = new List<IMidiInputDevice>();

        // Chain of the Lights track
        static List<object> _chain = new List<object>();

        // Access chain
        public static List<object> Chain {
            get {
                return _chain;
            }
        }

        // Initialize Program
        static void Main(string[] args) {
            _chain.Add(new Devices.Pitch(2)); // Debug

            // Initialize MIDI I/O
            foreach (var api in MidiDeviceManager.Default.GetAvailableMidiApis())
                Console.WriteLine($"Available API: {api}");
            
            Console.WriteLine("api done!");
            Console.WriteLine("dev count: " + MidiDeviceManager.Default.InputDevices.Count().ToString());

            foreach (var inputDeviceInfo in MidiDeviceManager.Default.InputDevices) {
                Console.WriteLine($"Opening {inputDeviceInfo.Name}");

                var inputDevice = inputDeviceInfo.CreateDevice();
                devices.Add(inputDevice);

                inputDevice.ControlChange += ControlChangeHandler;
                inputDevice.Open();

                Console.Read();
            }

            // Initialize API
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();
            
            host.Run(); // Halts the thread!
        }

        static void ControlChangeHandler(IMidiInputDevice sender, in ControlChangeMessage msg) {   
            Console.WriteLine($"[{sender.Name}] ControlChange: Channel:{msg.Channel} Control:{msg.Control} Value:{msg.Value}");
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
