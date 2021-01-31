using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Avalonia.Threading;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Structures;
using Apollo.Windows;

namespace Apollo.Helpers {
    public static class AbletonConnector {
        static readonly IPAddress localhost = new IPAddress(new byte[] {127, 0, 0, 1});
        const int Port = 1548;

        static UdpClient connection;
        static object locker = new object();
        static bool handlingFileOpen;

        static Dictionary<IPEndPoint, AbletonLaunchpad> portMap = new();

        static void Receive(IAsyncResult result) {
            lock (locker) {
                if (connection == null || result == null) return;

                IPEndPoint source = null;
                byte[] message = connection?.EndReceive(result, ref source);

                connection?.BeginReceive(new AsyncCallback(Receive), connection);

                if (!source.Address.Equals(localhost)) return;

                if (!portMap.ContainsKey(source)) {
                    if (message[0] == 247) {
                        App.Args = new [] { Encoding.UTF8.GetString(message.Skip(1).ToArray()) };

                        if (handlingFileOpen) return;
                        handlingFileOpen = true;
                        
                        Dispatcher.UIThread.InvokeAsync(async () => {
                            if (Program.Project == null) App.Windows.OfType<SplashWindow>().First().ReadFile(App.Args[0]);
                            else await Program.Project.AskClose();
                            
                            App.Args = null;
                            handlingFileOpen = false;
                        });
                    
                    } else if (message[0] >= 242 && message[0] <= 244)
                        connection?.SendAsync(new byte[] {244, Convert.ToByte((portMap[source] = MIDI.ConnectAbleton(244 - message[0])).Name.Substring(18))}, 2, source);

                } else if (message[0] < 128)
                    portMap[source].HandleNote(message[0], message[1]);
                    
                else if (message[0] == 245) {
                    MIDI.Disconnect(portMap[source]);
                    portMap.Remove(source);
                
                } else if (message[0] == 246 && Program.Project != null)
                    Program.Project.BPM = BitConverter.ToUInt16(message, 1);
            }
        }

        static void Send(AbletonLaunchpad source, byte[] data) =>
            connection?.SendAsync(data, data.Length, portMap.First(x => x.Value == source).Key);

        public static void Send(AbletonLaunchpad source, Signal n) =>
            Send(source, new byte[] {Converter.XYtoDR(n.Index), (byte)(n.Color.Max * 127.0 / 63)});

        public static void SendClear(AbletonLaunchpad source) =>
            Send(source, new byte[] {0xB0, 0x78, 0x00});

        public static bool Connected => 
            #if PRERELEASE
                true;
            #else
                connection != null;
            #endif

        static AbletonConnector() {
            try {
                connection = new UdpClient(Port);
            } catch {}

            connection?.BeginReceive(new AsyncCallback(Receive), connection);
        }

        public static void Dispose() {
            lock (locker) {
                portMap.Where(i => i.Value.Version >= 2).ToList()
                    .ForEach(i => connection?.SendAsync(new byte[] {245}, 1, i.Key));

                connection?.Close();
                connection = null;
            }
        }

        public static void NewInstanceFile(string path) {
            byte[] data = new byte[] { 247 }.Concat(Encoding.UTF8.GetBytes(path)).ToArray();

            using (UdpClient client = new UdpClient()) {
                client.Connect(new IPEndPoint(localhost, Port));
                client.Send(data, data.Length);
            }
        }
    }
}