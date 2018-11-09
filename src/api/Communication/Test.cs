using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;

namespace api.Communication
{
    [Route("api/[controller]")]
    public class ContactsController : Controller
    {
        // GET api/contacts
        [HttpGet]
        public IActionResult Get()
        {
            var result = new [] {
                new { FirstName = "John", LastName = "Doe" },
                new { FirstName = "Mike", LastName = "Smith" }
            };

            return Ok(result);
        }
    }

    public class Frontend {
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