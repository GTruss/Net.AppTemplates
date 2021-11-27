using Autofac.Extensions.DependencyInjection;

using App.Infrastructure.Data;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

namespace App.Web.Api {
    public class Program {
        public static int Main(string[] args) {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope()) {
                var services = scope.ServiceProvider;

                try {
                    var context = services.GetRequiredService<AppDbContext>();
                    //                    context.Database.Migrate();
                    context.Database.EnsureCreated();
                    SeedData.Initialize(services);
                    host.Run();
                    return 0;
                }
                catch {
                    return 1;
                    //var logger = services.GetRequiredService<ILogger<Program>>();
                    //logger.LogError(ex, "An error occurred seeding the DB.");
                }
            }

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder
                        .UseStartup<Startup>()
                        .UseSerilog();
                        //.ConfigureLogging(logging => {
                        //    logging.ClearProviders();
                        //    logging.AddConsole();
                            // logging.AddAzureWebAppDiagnostics(); add this if deploying to Azure
                        //});
        });

    }
}
