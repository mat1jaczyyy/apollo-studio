using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace api.Communication.Calls {
    [Route("[controller]")] public class ApiController: Controller {
        [HttpPost] public IActionResult Post() {
            byte[] buffer = new byte[Convert.ToInt32(this.Request.ContentLength)];
            this.Request.Body.Read(buffer, 0, Convert.ToInt32(this.Request.ContentLength));
            return Set.Request(Encoding.UTF8.GetString(buffer));
        }
    }
}