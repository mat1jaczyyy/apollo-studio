using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;

using api.Outbreak;

namespace api.Controllers {
    [Route("api/[controller]")]
    public class SetController : Controller {
        // TODO: Returns the entire chain of the Set.
        // GET api/set
        [HttpGet]
        public IActionResult Get() {
            var result = new List<object>();

            foreach (object Device in Program.Chain) {
                if (Device.GetType() == typeof(Outbreak.Lightweight)) {
                    result.Add(new { Path = ((Outbreak.Lightweight) Device).Path, FileName = ((Outbreak.Lightweight) Device).FileName });
                }
            }

            return Ok(result.ToArray());
        }
    }
}