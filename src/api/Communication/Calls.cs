using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace api.Communication.Calls {
    [Route("[controller]")] public class Add_DeviceController: Controller {
        [HttpPost] public IActionResult Post() {
            byte[] buffer = new byte[Convert.ToInt32(this.Request.ContentLength)];
            this.Request.Body.Read(buffer, 0, Convert.ToInt32(this.Request.ContentLength));

            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(buffer));

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