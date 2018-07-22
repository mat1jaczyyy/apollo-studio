using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Commons.Music.Midi;
using api.Devices;

namespace api {
    class Program {
        // MIDI Access
        static IMidiAccess access = MidiAccessManager.Default;
        static IMidiInput input;
        static IMidiOutput output;

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

            Console.WriteLine(access.Inputs.Last().Id);
            Console.WriteLine(access.Outputs.Last().Id);

            input = access.OpenInputAsync(access.Inputs.Last().Id).Result;
            output = access.OpenOutputAsync(access.Outputs.Last().Id).Result;

            output.Send(new byte[] {0xB0, 0x68, 0x05}, 0, 3, 0);

            // Initialize API
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();
            
            host.Run(); // Halts the thread!
        }

        static void MIDIIn(object sender, MidiReceivedEventArgs e) {
            for (int i = 0; i < e.Length; i++) {
                Console.WriteLine(e.Data[i].ToString());
            }
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
