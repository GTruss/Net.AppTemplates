using Microsoft.Extensions.Configuration;

using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.MSSqlServer;

using System;
using My.Shared.Logging.Serilog;

namespace App.Win {
    public static class SerilogConfig {
        public static void Configure(IConfiguration config, out InMemorySink memSink, out InMemorySink flatSink) {
            string logFileName = AppDomain.CurrentDomain.BaseDirectory + @$"\logs\LogFile_{DateTime.Now:yyyyMMdd_HHmmss}.log";
            memSink = new InMemorySink("[{Timestamp:HH:mm:ss.fff} ({EventType}) {Level:u3}] {Message:lj}{NewLine}{Exception}");
            flatSink = new InMemorySink("{Timestamp:HH:mm:ss.fff}|{EventType}|{Level:u3}|{Message:lj}|{Exception}");

            Log.Logger = new LoggerConfiguration()
                            .ReadFrom.Configuration(config)
                            .Enrich.FromLogContext()
                            .Enrich.WithMachineName()
                            .Enrich.With<EventTypeEnricher>()
                            .WriteTo.Sink(memSink)
                            .WriteTo.Sink(flatSink)
                            .WriteTo.File(logFileName, outputTemplate: "[{Timestamp:HH:mm:ss.fff} ({EventType}) {Level:u3}] {Message:lj}{NewLine}{Exception}")
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
}
