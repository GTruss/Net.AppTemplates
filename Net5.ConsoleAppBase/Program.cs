using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Net5.Common;
using Serilog;
using System;
using System.IO;

// Sets up DI, Logging (Serilog) and Configuration Settings (ConfigurationBuilder with appsettings files)

namespace Net5.ConsoleAppBase {
    /// <summary>
    /// .bat file needed to set environment variables on the server
    /// setx DOTNET_ENVIRONMENT Development /M
    /// setx DOTNET_ENVIRONMENT Staging /M
    /// -- No reason to set "Production" because it's the fallback if the environment variable isn't set.
    /// </summary>
    class Program {
        static readonly string env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        static readonly bool isDev = env == "Development";
        static Serilog.ILogger _logger;
        static IConfiguration _config;
    
        static void Main(string[] args) {
            DateTime startTime = DateTime.Now;
            try {

                var builder = new ConfigurationBuilder();
                _config = BuildConfig(builder);

                CleanupExpiredLogs();

                // Setup Serilog
                SerilogConfig.Configure(_config);
                _logger = Log.Logger;
                _logger.Information("Starting...");
                
                if (string.IsNullOrEmpty(env) ||
                    !env.Contains("Development") && !env.Contains("Staging") && !env.Contains("Production")) {
                    _logger.Error("Missing/Unexpected environment variable: DOTNET_ENVIRONMENT = {env} (expected 'Development', 'Staging' or 'Production').", env);
                    return;
                }

                // Setup DI
                var host = DIContainerConfig.Configure(_config);

                // Create a new local scope and run the Main service
                using (var scope = host.Services.CreateScope()) {
                    var svc = scope.ServiceProvider.GetRequiredService<MainService>();
                    svc.Run();
                };

            }
            catch (Exception ex) {
                _logger.Fatal(ex, "Application Erorr");
            }
            finally {
                Finalize(startTime);
                Log.CloseAndFlush();
            }
        }

        static void Finalize(DateTime startTime) {
            DateTime endTime = DateTime.Now;
            TimeSpan diff = endTime - startTime;

            _logger.Information("Time - {diff}", diff);
            _logger.Information("Complete");

            if (isDev) {
                Console.WriteLine("Hit ENTER to exit");
                Console.ReadLine();
            }
        }

        static IConfiguration BuildConfig(IConfigurationBuilder builder) {
            // Setup Settings
            return builder.SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                          .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json",
                                       optional: true, reloadOnChange: true)
                          .AddEnvironmentVariables()
                          .Build();
        }

        static void CleanupExpiredLogs() {
            try {
                string expireInterval = _config.GetValue<string>("Log:Expiration:Interval");
                int expireCount = _config.GetValue<int>("Log:Expiration:Count");
                string path = AppDomain.CurrentDomain.BaseDirectory;
                path += @"\logs";

                var files = Directory.GetFiles(path);

                foreach (var fileName in files) {
                    var file = new FileInfo(fileName);
                    DateTime threshold = DateTime.Now;

                    switch (expireInterval.ToLower()) {
                        case "minutes":
                            threshold = DateTime.Now.AddMinutes(-expireCount);
                            break;

                        case "days":
                            threshold = DateTime.Now.AddDays(-expireCount);
                            break;
                    }

                    if (file.CreationTime < threshold) {
                        file.Delete();
                    }
                }
            }
            catch {
                //The log file directory doesn't exist yet, ignore
                return;
            }
        }
    }
}
