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
        static IMidiInputDevice iDevice;
        static IMidiOutputDevice oDevice;
        public static Chain _chain = new Chain(MIDIExit);
        public static bool log = false;

        static void Main(string[] args) {
            foreach (string arg in args)
                if (arg.Equals("--log"))
                    log = true;

            _chain.Add(
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
                        new Group(new Chain[] {
                            new Chain(new Device[] {
                                new Paint(new Color(63, 0, 0))
                            }),
                            new Chain(new Device[] {
                                new Delay(100),
                                new Paint(new Color(63, 15, 0))
                            }),
                            new Chain(new Device[] {
                                new Delay(200),
                                new Paint(new Color(63, 63, 0))
                            }),
                            new Chain(new Device[] {
                                new Delay(300),
                                new Paint(new Color(0, 63, 0))
                            }),
                            new Chain(new Device[] {
                                new Delay(400),
                                new Paint(new Color(0, 63, 63))
                            }),
                            new Chain(new Device[] {
                                new Delay(500),
                                new Paint(new Color(0, 0, 63))
                            }),
                            new Chain(new Device[] {
                                new Delay(600),
                                new Paint(new Color(7, 0, 63))
                            }),
                            new Chain(new Device[] {
                                new Delay(700),
                                new Paint(new Color(0))
                            })
                        })
                    }),
                    new Chain(new Range(51, 88)),
                    new Chain(new Range(18, 18), new Device[] {new Lightweight("/Users/mat1jaczyyy/Downloads/break2.mid")})
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

            var host = new WebHostBuilder().UseKestrel().UseStartup<Startup>().Build();
            
            host.Run();
        }

        static void MIDIExit(Signal n) {
            byte[] data = {0x00, 0x20, 0x29, 0x02, 0x18, 0x0B, n.Index, n.Color.Red, n.Color.Green, n.Color.Blue};
            SysExMessage msg = new SysExMessage(data);
            
            if (log)
                Console.WriteLine($"OUT <- {msg.ToString()}");

            oDevice.Send(in msg);
        }

        static void NoteOn(object sender, in NoteOnMessage e) {
            if (log)
                Console.WriteLine($"IN  -> {e.Key.ToString()} {e.Velocity.ToString()}");

            _chain.MIDIEnter(new Signal(e.Key, new Color((byte)(e.Velocity >> 1))));
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
