using System;
using System.IO;
using System.Windows.Forms;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Autofac.Extensions.DependencyInjection;
using Autofac;
using My.Shared.Logging.Serilog;
using App.Infrastructure;
using App.Core;

// Sets up DI, Logging (Serilog) and Configuration Settings (ConfigurationBuilder with appsettings.*.json files)

// .bat file needed to set appropriate environment variable on the server,
// or run from cmd /k
// setx DOTNET_ENVIRONMENT Development /M
// setx DOTNET_ENVIRONMENT Staging /M
// setx DOTNET_ENVIRONMENT Production /M

namespace App.Win;

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
        _config = CreateConfiguration(builder);

        // Setup Serilog          
        SerilogConfig.Configure(_config, out InMemorySink memSink, out InMemorySink flatSink);
        _logger = Log.Logger;
        Log.ForContext("SourceContext", "Program")
           .Information("Windows App Startup");

        // Auto-delete old log files. Configurable for per environment.
        CleanupExpiredLogs();

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
        _host = CreateHost(_config);

        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using (var scope = _host.Services.CreateScope()) {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<MainForm>>();
            var mainForm = new MainForm(scope, logger, _config, memSink.Events, flatSink.Events);
            Application.Run(mainForm);
        };
    }

    static IConfiguration CreateConfiguration(IConfigurationBuilder builder) {
        // Setup Settings
        return builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env}.json",
                            optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
    }

    public static IHost CreateHost(IConfiguration config) {
        var host = Host.CreateDefaultBuilder()
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureServices((context, services) => {
                services.AddTransient<IConfiguration>(context => config);
                services.AddSQLServerDbContext(config.GetConnectionString("Sandbox"));
            })
            .ConfigureContainer<ContainerBuilder>(container => {
                container.RegisterModule(new DefaultServicesModule());
                container.RegisterModule(new DefaultInfrastructureModule(env == "Development"));
            })
            .UseSerilog()
            .Build();

        return host;
    }

    static void CleanupExpiredLogs() {
        try {
            LogArchive logSettings = new();
            _config.Bind("LogArchive", logSettings);

            Log.ForContext("SourceContext", "Program")
               .ForContext("LogArchive", logSettings)
               .Information("CleanupExpiredLogs()");

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
        catch (Exception ex) {
            Log.Error(ex, ex.Message);
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

