using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace api.Communication {
    public static class Server {
        private static readonly string ip = "localhost";
        private static readonly ushort port = 1548;

        private static IWebHost host = new WebHostBuilder()
            .UseKestrel()
            .UseStartup<Startup>()
            .UseUrls($"http://{ip}:{port}/")
            .SuppressStatusMessages(!api.Program.log)
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

            public void Configure(IApplicationBuilder app) {
                app.UseCors("AllowAllOrigins");
                app.UseMvc();
            }
        }

        public static async void Start() {
            await Task.Run(() => {
                host.Run();
            });
        }
    }

    [Route("/[controller]")] public class ApiController: Controller {
        [HttpPost()] public IActionResult Post() {
            byte[] buffer = new byte[Convert.ToInt32(this.Request.ContentLength)];
            this.Request.Body.Read(buffer, 0, Convert.ToInt32(this.Request.ContentLength));

            string request = Encoding.UTF8.GetString(buffer);
            api.Program.Log($"REQ -> {request}");

            ObjectResult response = (request == "")? new OkObjectResult(null) : Set.Request(request);
            api.Program.Log($"RSP <- {response.Value.ToString()}"); 

            return response;
        }
    }
}