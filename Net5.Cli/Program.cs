using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.Extensions.Hosting;

using Net5.DependencyInjection;

// Sets up DI, Logging (Serilog) and Configuration Settings (ConfigurationBuilder with appsettings.*.json files)

// .bat file needed to set appropriate environment variable on the server,
// or run from cmd /k
// setx DOTNET_ENVIRONMENT Development /M
// setx DOTNET_ENVIRONMENT Staging /M
// setx DOTNET_ENVIRONMENT Production /M

namespace Net5.Cli {
    class Program {

        static readonly string env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        static readonly bool isDev = env == "Development";
        static IHost _host;
        static ILogger _logger;
        static IConfiguration _config;
    
        /// <summary>
        /// This is the Composition Root of this application.
        /// Only app & cross-cutting concerns setup should go here (appsettings, logging, DI, etc).
        /// Call appropriate Services as defined in Net5.ApplicationServices.
        /// This eliminates the need for Services to have references to every dependency, as 
        ///   the DI Composition Root does. Allows for more flexible late-binding. Why should the
        ///   Main service have a dependency to SQL and Mongo and Oracle and (n) repositories, for example. It shouldn't care.
        ///   So, the CLI, in this case, will have dependencies to everything for the DI container
        ///   but the actual driver "MainService" shouldn't.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args) {
            DateTime startTime = DateTime.Now;
            try {

                var builder = new ConfigurationBuilder();
                _config = BuildConfig(builder);

                // Auto-delete old log files. Configurable for each environment.
                CleanupExpiredLogs();

                // Setup Serilog
                SerilogConfig.Configure(_config);
                _logger = Log.Logger;
                _logger.Information("Starting...");
                
                if (string.IsNullOrEmpty(env) ||
                    !env.Contains("Development") && !env.Contains("Staging") && !env.Contains("Production")) {
                    _logger.Error("Missing/Unexpected environment variable: DOTNET_ENVIRONMENT = {env} (expected 'Development', 'Staging' or 'Production').", env);
                    // Microsoft's default is to "fallback" to Production if the DOTNET_ENVIRONMENT
                    // doesn't exist. This is bad. If it's a manual deployment to the Development or Staging
                    // environment and they forget to create the Environment Variable, it will run the Production
                    // configuration BY DEFAULT! Bad Microsoft. Bad. People make mistakes and skip steps. Err on the side of caution.
                    // So, require that the Environment Variable to be set AND it must be one of the three.
                    return;
                }

                // Setup DI
                _host = AppHostContainer.Configure(_config);

                // Create a new local scope and run the Main service
                using (var scope = _host.Services.CreateScope()) {
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
                LogArchive logSettings = new();
                _config.Bind("LogArchive", logSettings);

                string path = AppDomain.CurrentDomain.BaseDirectory;
                path += @"\logs";

                var files = Directory.GetFiles(path);

                foreach (var fileName in files) {
                    var file = new FileInfo(fileName);
                    DateTime threshold = DateTime.Now;

                    threshold = logSettings.Expiration.Interval.ToLower() switch {
                        "minutes" => DateTime.Now.AddMinutes(-logSettings.Expiration.IntervalCount),
                        "days" => DateTime.Now.AddDays(-logSettings.Expiration.IntervalCount),
                        _ => throw new NotImplementedException()
                    };

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

    public record LogArchive {
        public ExpirationSettings Expiration { get; set; }

        public record ExpirationSettings {
            public string Interval { get; set; }
            public int IntervalCount { get; set; }
        }
    }
}
