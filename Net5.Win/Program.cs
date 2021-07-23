using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

using Net5.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Net5.Cli;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Net5.Common.Serilog;

namespace Net5.Win {
    static class Program {

        #pragma warning disable CS8601 // Possible null reference assignment.
        static readonly string env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        static readonly bool isDev = env == "Development";
        static IHost _host;
        static Serilog.ILogger _logger;
        static IConfiguration _config;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {

            var builder = new ConfigurationBuilder();
            _config = BuildConfig(builder);

            // Setup Serilog          
            var sink = SerilogConfig.Configure(_config);
            _logger = Log.Logger;
            _logger.Information("Windows App Startup");

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

            _host = AppHostContainer.Configure(_config);

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var scope = _host.Services.CreateScope()) {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<MainForm>>();
                var mainForm = new MainForm(scope, logger, _config, sink.Events);
                Application.Run(mainForm);
            };
        }

        static IConfiguration BuildConfig(IConfigurationBuilder builder) {
            // Setup Settings
            return builder.SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                          .AddJsonFile($"appsettings.{env}.json",
                                       optional: false, reloadOnChange: true)
                          .AddEnvironmentVariables()
                          
                          .Build();
        }
    }
}
