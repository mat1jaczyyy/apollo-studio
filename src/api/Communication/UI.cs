using System;
using System.Net.Http;
using System.Text;

namespace api.Communication {
    public class UI {
        private static readonly string ip = "localhost";
        private static readonly ushort port = 1549;

        public static async void Init() {
            using (HttpClient client = new HttpClient())
                await client.PostAsync($"http://{ip}:{port}/init", new StringContent(Set.Encode(), Encoding.UTF8, "application/json"));
        }
    }
}