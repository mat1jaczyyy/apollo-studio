using System;
using System.IO;

using DiscordRPC;

using Apollo.Structures;

namespace Apollo.Core {
    public static class Discord {
        static bool Initialized = false;
        static DiscordRpcClient Presence;
        static Timestamps Time;
        
        static Courier courier = new Courier() { Interval = 1000 };
        static object locker = new object();

        static Discord() => courier.Elapsed += Refresh;

        public static void Init() {
            if (Initialized) new InvalidOperationException("Discord Refresh is already running");

            Presence = new DiscordRpcClient("579791801527959592");
            Presence.Initialize();
            Time = new Timestamps(DateTime.UtcNow);
            Initialized = true;

            Refresh();
            courier.Start();
        }

        public static void Refresh(object sender = null, EventArgs e = null) {
            lock (locker) {
                if (Initialized) {
                    RichPresence Info = new RichPresence() {
                        Details = Program.Version,
                        Timestamps = Time,
                        Assets = new Assets() {
                            LargeImageKey = "logo"
                        }
                    };

                    if (Program.Project != null)
                        Info.State = "Working on " + ((Program.Project.FilePath == "")? "a new Project" : Path.GetFileNameWithoutExtension(Program.Project.FilePath));

                    Presence.SetPresence(Info);
                }
            }
        }

        public static void Dispose() {
            lock (locker) {
                courier.Stop();

                Initialized = false;
                Presence.Dispose();
            }
        }
    }
}