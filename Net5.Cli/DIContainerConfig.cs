using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Net5.Cli {
    internal static class DIContainerConfig {
        public static IHost Configure(IConfiguration config) {
            var host = Host.CreateDefaultBuilder()
                        .ConfigureServices((context, services) => {
                            services.AddTransient<MainService>();
                            //services.AddDbContext<YourDataContext>(options =>
                            //    options.UseSqlServer(config.GetConnectionString("DefaultConnection"))
                            //);
                        })
                        .UseSerilog()
                        .Build();

            return host;
        }
    }
}
