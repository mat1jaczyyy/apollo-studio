using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using RtMidi.Core.Enums;
using RtMidi.Core.Messages;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Helpers {
    public static class AbletonConnector {
        static readonly IPAddress localhost = new IPAddress(new byte[] {127, 0, 0, 1});
        static UdpClient connection;

        static Dictionary<IPEndPoint, AbletonLaunchpad> portMap = new Dictionary<IPEndPoint, AbletonLaunchpad>();

        static void Receive(IAsyncResult result) {
            if (connection == null || result == null) return;

            IPEndPoint source = null;
            byte[] message = connection?.EndReceive(result, ref source);

            connection?.BeginReceive(new AsyncCallback(Receive), connection);

            if (source.Address.Equals(localhost)) {
                if (!portMap.ContainsKey(source) && message[0] >= 243 && message[0] <= 244)
                    connection?.SendAsync(new byte[] {244, Convert.ToByte((portMap[source] = MIDI.ConnectAbleton(244 - message[0])).Name.Substring(18))}, 2, source);

                if (message[0] < 128) {
                    NoteOnMessage msg = new NoteOnMessage(Channel.Channel1, (Key)message[0], message[1]);
                    portMap[source].NoteOn(null, in msg);
                    
                } else if (message[0] == 245) {
                    MIDI.Disconnect(portMap[source]);
                    portMap.Remove(source);
                }
            }
        }

        private static void Send(AbletonLaunchpad source, byte[] data) =>
            connection?.SendAsync(data, data.Length, portMap.First(x => x.Value == source).Key);

        public static void Send(AbletonLaunchpad source, Signal n) =>
            Send(source, new byte[] {Converter.XYtoDR(n.Index), (byte)(n.Color.Max * 127.0 / 63)});

        public static void SendClear(AbletonLaunchpad source) =>
            Send(source, new byte[] {0xB0, 0x78, 0x00});

        public static bool Connected => connection != null;

        static AbletonConnector() {
            try {
                connection = new UdpClient(1548);
            } catch {}

            connection?.BeginReceive(new AsyncCallback(Receive), connection);
        }

        public static void Dispose() {
            connection?.Close();
            connection = null;
        }
    }
}