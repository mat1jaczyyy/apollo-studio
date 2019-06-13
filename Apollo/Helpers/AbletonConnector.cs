using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using RtMidi.Core.Enums;
using RtMidi.Core.Messages;

using Apollo.Core;
using Apollo.Elements;

namespace Apollo.Helpers {
    public static class AbletonConnector {
        static readonly IPAddress localhost = new IPAddress(new byte[] {127, 0, 0, 1});
        static UdpClient connection;

        static Dictionary<int, AbletonLaunchpad> portMap = new Dictionary<int, AbletonLaunchpad>();

        private static void Receive(IAsyncResult result) {
            IPEndPoint source = null;
            byte[] message = connection.EndReceive(result, ref source);

            connection.BeginReceive(new AsyncCallback(Receive), connection);

            if (source.Address.Equals(localhost)) {
                Program.Log($"{source} => {string.Join(", ", message)}");

                if (!portMap.ContainsKey(source.Port))
                    portMap[source.Port] = MIDI.ConnectAbleton();

                if (message[0] < 128) {
                    NoteOnMessage msg = new NoteOnMessage(Channel.Channel1, (Key)message[0], message[1]);
                    portMap[source.Port].NoteOn(null, in msg);
                    
                } else if (message[0] == 245) {
                    MIDI.Disconnect(portMap[source.Port]);
                    portMap.Remove(source.Port);
                }
            }
        }

        public static bool Connected => connection != null;

        static AbletonConnector() {
            try {
                connection = new UdpClient(1548);
            } catch {}

            connection?.BeginReceive(new AsyncCallback(Receive), connection);
        }
    }
}