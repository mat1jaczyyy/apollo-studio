using System;
using System.Net.Http;

namespace api.Communication {
    public class UI {
        public static async void Test() {
            using (HttpClient client = new HttpClient()) {
                using (HttpResponseMessage res = await client.GetAsync("http://localhost:1549")) {
                    using (HttpContent content = res.Content) {
                        string data = await content.ReadAsStringAsync();
                        if (data != null) {
                            Console.WriteLine(data);
                        }
                    }
                }
            }
        }
    }
}