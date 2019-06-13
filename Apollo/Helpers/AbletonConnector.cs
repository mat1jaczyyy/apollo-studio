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

        private static void Receive(IAsyncResult result) {
            IPEndPoint source = null;
            byte[] message = connection.EndReceive(result, ref source);

            connection.BeginReceive(new AsyncCallback(Receive), connection);

            if (source.Address.Equals(localhost)) {
                Program.Log($"{source} => {string.Join(", ", message)}");

                if (!portMap.ContainsKey(source))
                    portMap[source] = MIDI.ConnectAbleton();

                if (message[0] < 128) {
                    NoteOnMessage msg = new NoteOnMessage(Channel.Channel1, (Key)message[0], message[1]);
                    portMap[source].NoteOn(null, in msg);
                    
                } else if (message[0] == 245) {
                    MIDI.Disconnect(portMap[source]);
                    portMap.Remove(source);
                }
            }
        }

        public static void Send(AbletonLaunchpad source, Signal n) =>
            connection?.SendAsync(new byte[] {Converter.XYtoDR(n.Index), (byte)(n.Color.Max * 127.0 / 63)}, 2, portMap.First(x => x.Value == source).Key);

        public static bool Connected => connection != null;

        static AbletonConnector() {
            try {
                connection = new UdpClient(1548);
            } catch {}

            connection?.BeginReceive(new AsyncCallback(Receive), connection);
        }
    }
}