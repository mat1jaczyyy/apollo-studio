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
    class Program {
        public static List<Track> _tracks = new List<Track>();
        public static bool log = false;

        static void Main(string[] args) {
            foreach (var api in MidiDeviceManager.Default.GetAvailableMidiApis())
                Console.WriteLine($"MIDI API: {api}");

            foreach (string arg in args)
                if (arg.Equals("--log"))
                    log = true;

            _tracks.Add(new Track());
            _tracks[0].Chain.Add(
                new Group(new Chain[] {
                    new Chain(new Range(11, 11), new Device[] {
                        new Translation(-127),
                        new Translation(44),
                        new Infinity(),
                        new Group(new Chain[] {
                            new Chain(),
                            new Chain(new Device[] {
                                new Delay(100),
                                new Translation(-1),
                                new Duplication(new int[] {-9})
                            }),
                            new Chain(new Device[] {
                                new Delay(200),
                                new Translation(-2),
                                new Duplication(new int[] {-9, -18})
                            }),
                            new Chain(new Device[] {
                                new Delay(300),
                                new Translation(-3),
                                new Duplication(new int[] {-9, -18, -27})
                            }),
                            new Chain(new Device[] {
                                new Delay(400),
                                new Translation(-13),
                                new Duplication(new int[] {-9, -18})
                            }),
                            new Chain(new Device[] {
                                new Delay(500),
                                new Translation(-23),
                                new Duplication(new int[] {-9})
                            }),
                            new Chain(new Device[] {
                                new Delay(600),
                                new Translation(-33)
                            })
                        }),
                        new Iris(100, new Color[] {
                            new Color(63, 0, 0),
                            new Color(63, 15, 0),
                            new Color(63, 63, 0),
                            new Color(0, 63, 0),
                            new Color(0, 63, 63),
                            new Color(0, 0, 63),
                            new Color(7, 0, 63)
                        })
                    }),
                    new Chain(new Range(51, 88)),
                    new Chain(new Range(18, 18), new Device[] {new Lightweight("/Users/mat1jaczyyy/Downloads/break2.mid")})
                })
            );

            /*_tracks.Add(new Track());
            _tracks[1].Chain = _tracks[0].Chain.Clone();*/

            var host = new WebHostBuilder().UseKestrel().UseStartup<Startup>().Build();
            host.Run();
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
