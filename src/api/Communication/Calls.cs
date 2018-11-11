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
    class Parser {
        public static Dictionary<string, object> ParseRequest(HttpRequest request) {
            byte[] buffer = new byte[Convert.ToInt32(request.ContentLength)];
            request.Body.Read(buffer, 0, Convert.ToInt32(request.ContentLength));
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(buffer));
        }
    }
    

    [Route("[controller]")] public class Add_DeviceController: Controller {
        [HttpPost] public IActionResult Post() {
            var json = Parser.ParseRequest(this.Request);

            foreach (Type device in (from type in Assembly.GetExecutingAssembly().GetTypes() where (type.Namespace.StartsWith("api.Devices") && !type.Namespace.StartsWith("api.Devices.Device")) select type)) {
                if (device.Name.ToLower().Equals(json["device"])) {
                    Set.Tracks[Convert.ToInt32(json["track"])].Chain.Insert(Convert.ToInt32(json["index"]), (Devices.Device)Activator.CreateInstance(device));
                    break;
                }
            }

            return Ok(Set.Tracks[Convert.ToInt32(json["track"])].Chain[Convert.ToInt32(json["index"])].EncodeSpecific());
        }
    }
}