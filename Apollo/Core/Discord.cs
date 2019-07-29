using System;

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

        public static void Set(bool state) {
            if (state) Init();
            else Dispose();
        }

        static void Init() {
            if (Initialized) return;

            Presence = new DiscordRpcClient("579791801527959592");
            Presence.Initialize();
            Time = new Timestamps(DateTime.UtcNow);
            Initialized = true;

            Refresh();
            courier.Start();
        }

        static void Refresh(object sender = null, EventArgs e = null) {
            lock (locker) {
                if (Initialized) {
                    RichPresence Info = new RichPresence() {
                        Details = Program.Version,
                        Timestamps = Time,
                        Assets = new Assets() {
                            LargeImageKey = "logo"
                        }
                    };

                    if (Preferences.DiscordFilename && Program.Project != null) {
                        string s = "Working on " + ((Program.Project.FilePath == "")? "a new Project" : Program.Project.FileName);
                        Info.State = (s.Length > 128)? s.Substring(0, 125) + "..." : s;
                    }

                    Presence.SetPresence(Info);
                }
            }
        }

        static void Dispose() {
            if (!Initialized) return;

            lock (locker) {
                courier.Stop();

                Initialized = false;
                Presence.ClearPresence();
                Presence.Dispose();
            }
        }
    }
}