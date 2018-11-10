using Microsoft.AspNetCore.Mvc;

namespace api.Communication.Calls {
    [Route("[controller]")] public class TestController: Controller {
        [HttpGet] public IActionResult Get() {
            var result = new [] {
                new { FirstName = "John", LastName = "Doe" },
                new { FirstName = "Mike", LastName = "Smith" }
            };

            return Ok(result);
        }
    }
}