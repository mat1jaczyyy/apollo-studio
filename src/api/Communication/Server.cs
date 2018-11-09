using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace api.Communication {
    public static class Server {
        private static IWebHost host = new WebHostBuilder()
            .UseKestrel()
            .UseStartup<Startup>()
            .UseUrls("http://localhost:1548/") // API = 1548, APP = 1549
            .Build();

        private class Startup {
            public Startup(IHostingEnvironment env) {}

            // This method gets called by the runtime. Use this method to add services to the container.
            public void ConfigureServices(IServiceCollection services) {
                // Add framework services.
                services.AddMvc().AddJsonOptions(options => {
                    //return json format with Camel Case
                    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
                });
            }

            // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
            public void Configure(IApplicationBuilder app) {
                app.UseMvc();
            }
        }

        public static async void Start() {
            await Task.Run(() => {
                host.Run();
            });
        }
    }
}