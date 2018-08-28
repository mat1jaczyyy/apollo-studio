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
                    }, new Range(11, 11)),
                    new Chain(new Range(51, 88)),
                    new Chain(new Device[] {new Lightweight("/Users/mat1jaczyyy/Downloads/break2.mid")}, new Range(18, 18))
                })
            );

            //_chain.Add(

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
            byte[] data = {0x00, 0x20, 0x29, 0x02, 0x18, 0x0B, n.Index, n.Red, n.Green, n.Blue};
            SysExMessage msg = new SysExMessage(data);
            //Console.WriteLine($"OUT <- {msg.ToString()}");

            oDevice.Send(in msg);
        }

        static void NoteOn(object sender, in NoteOnMessage e) {
            Console.WriteLine($"IN  -> {e.Key.ToString()} {e.Velocity.ToString()}");

            _chain.MIDIEnter(new Signal(e.Key, e.Velocity >> 1));
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
