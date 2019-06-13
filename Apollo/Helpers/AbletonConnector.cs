using System;
using System.Net;
using System.Net.Sockets;

using Apollo.Core;

namespace Apollo.Helpers {
    public static class AbletonConnector {
        static UdpClient connection;

        private static void Receive(IAsyncResult result) {
            IPEndPoint source = null;
            byte[] message = connection.EndReceive(result, ref source);

            Program.Log($"{source} => {string.Join(", ", message)}");

            connection.BeginReceive(new AsyncCallback(Receive), connection);
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