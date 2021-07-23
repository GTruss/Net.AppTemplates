using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Net5.Data.Sandbox.Entities;

using Serilog;

namespace Net5.Cli {
    internal static class DIContainerConfig {
        public static IHost Configure(IConfiguration config) {
            var host = Host.CreateDefaultBuilder()
                        .ConfigureServices((context, services) => {
                            services.AddTransient<MainService>();
                            services.AddDbContext<SandboxContext>(options =>
                                options.UseSqlServer(config.GetConnectionString("Sandbox"))
                            );
                        })
                        .UseSerilog()
                        .Build();

            return host;
        }
    }
}
