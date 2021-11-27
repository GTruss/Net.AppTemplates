using App.Infrastructure.Data;
using App.SharedKernel.Logging.Serilog;

using Humanizer.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog.Formatting.Json;

using Serilog;
using Serilog.Sinks.MSSqlServer;

namespace App.Infrastructure {
    public static class StartupSetup {
        public static void AddSQLServerDbContext(this IServiceCollection services, string connectionString) =>
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));
                
        public static void AddSQLiteDbContext(this IServiceCollection services, string connectionString) =>
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString)); // will be created in web project root

        public static void ConfigureLogger(IConfiguration configuration, string logFileName) {
            Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.With<EventTypeEnricher>()
                    .Enrich.With<SourceContextClassEnricher>()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} ({EventType}) {Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(logFileName, outputTemplate: "[{Timestamp:HH:mm:ss.fff} ({EventType}) {Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(new JsonFormatter(), logFileName + ".json")
                    .WriteTo.MSSqlServer(
                        configuration.GetConnectionString("SQLServerConnection"),
                        sinkOptions: new MSSqlServerSinkOptions() {
                            AutoCreateSqlTable = true,
                            TableName = "Log",
                            SchemaName = "dbo"
                        },
                        appConfiguration: configuration,
                        columnOptionsSection: configuration.GetSection("Serilog:ColumnOptions"),
                        logEventFormatter: new JsonFormatter()
                    )
                    .CreateLogger();

        }
    }               
}
