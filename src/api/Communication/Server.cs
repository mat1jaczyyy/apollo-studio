using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

using api.Core;
using api.Elements;

namespace api.Communication {
    public static class Server {
        private static readonly string ip = "localhost";
        private static readonly ushort port = 1548;

        private static IWebHost host = new WebHostBuilder()
            .UseKestrel()
            .UseStartup<Startup>()
            .UseUrls($"http://{ip}:{port}/")
            .SuppressStatusMessages(!Program.log)
            .Build();

        private class Startup {
            public Startup(IHostingEnvironment env) {}

            public void ConfigureServices(IServiceCollection services) {
                services.AddCors(options => {
                    options.AddPolicy("AllowAllOrigins", builder => { // Required for Frontend to be able to properly access API.
                        builder.AllowAnyOrigin().AllowAnyMethod();
                    });
                });

                services.AddMvc();
            }

            public void Configure(IApplicationBuilder app, IApplicationLifetime applicationLifetime) {
                app.UseCors("AllowAllOrigins");
                app.UseMvc();

                applicationLifetime.ApplicationStarted.Register(InitializeApplication);
            }

            public void InitializeApplication() { 
                Communication.UI.Init();
            }
        }

        public static async void Start() {
            await Task.Run(() => {
                host.Run();
            });
        }
    }

    [Route("/[controller]")] public class ApiController: Controller {
        private ObjectResult Respond(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

            string obj = json["object"].ToString();
            if (obj != "message") return new BadRequestObjectResult("Not a message.");
            
            return Set.Respond(
                obj,
                json["path"].ToString().Split('/'),
                JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString())
            );
        }

        [HttpPost()] public IActionResult Post() {
            byte[] buffer = new byte[Convert.ToInt32(this.Request.ContentLength)];
            this.Request.Body.Read(buffer, 0, Convert.ToInt32(this.Request.ContentLength));

            string request = Encoding.UTF8.GetString(buffer);
            Program.Log($"REQ -> {request}");

            ObjectResult response = (this.Request.ContentLength == 0)? new OkObjectResult(null) : Respond(request);
            if (response.Value != null) Program.Log($"RSP <- {response.Value.ToString()}"); 

            return response;
        }
    }
}