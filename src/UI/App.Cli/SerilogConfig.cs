using System;
using Microsoft.Extensions.Configuration;

using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.MSSqlServer;

using My.Shared.Logging.Serilog;

namespace App.Cli;

public static class SerilogConfig {
    public static void Configure(IConfiguration config) {
        string logFileName = AppDomain.CurrentDomain.BaseDirectory + @$"\logs\LogFile_{ DateTime.Now:yyyyMMdd_HHmmss}.log";
        Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(config)
                        .Enrich.FromLogContext()
                        .Enrich.WithMachineName()
                        .Enrich.With<EventTypeEnricher>()
                        .Enrich.With<SourceContextClassEnricher>()
                        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} ({EventType}) {Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
                        .WriteTo.File(logFileName, outputTemplate: "[{Timestamp:HH:mm:ss.fff} ({EventType}) {Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
                        .WriteTo.File(new JsonFormatter(), logFileName + ".json")
                        .WriteTo.MSSqlServer(
                            config.GetConnectionString("Sandbox"),                                
                            sinkOptions: new MSSqlServerSinkOptions() {
                                AutoCreateSqlTable = true,
                                TableName = "Log",
                                SchemaName = "dbo"
                            },
                            appConfiguration: config,
                            columnOptionsSection: config.GetSection("Serilog:ColumnOptions"),
                            logEventFormatter: new JsonFormatter()
                        )
                        .CreateLogger();
    }
}
