using Microsoft.Extensions.Configuration;

using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.MSSqlServer;

using Net5.Common.Serilog;
using System;

namespace Net5.Win {
    public static class SerilogConfig {
        public static InMemorySink Configure(IConfiguration config) {
            string logFileName = AppDomain.CurrentDomain.BaseDirectory + @$"\logs\LogFile_{DateTime.Now:yyyyMMdd_HHmmss}.log";
            var sink = new InMemorySink("[{Timestamp:HH:mm:ss.fff} ({EventType}) {Level:u3}] {Message:lj}{NewLine}{Exception}");

            Log.Logger = new LoggerConfiguration()
                            .ReadFrom.Configuration(config)
                            .Enrich.FromLogContext()
                            .Enrich.WithMachineName()
                            .Enrich.With<EventTypeEnricher>()
                            .WriteTo.Sink(sink)
                            .WriteTo.File(logFileName, outputTemplate: "[{Timestamp:HH:mm:ss.fff} ({EventType}) {Level:u3}] {Message:lj}{NewLine}{Exception}")
                            .WriteTo.MSSqlServer(
                                config.GetConnectionString("Sandbox"),                                
                                sinkOptions: new MSSqlServerSinkOptions() {
                                    AutoCreateSqlTable = true,
                                    TableName = "tbl_Log",
                                    SchemaName = "dbo"
                                },
                                appConfiguration: config,
                                columnOptionsSection: config.GetSection("Serilog:ColumnOptions"),
                                logEventFormatter: new JsonFormatter()
                            )
                            .CreateLogger();
            return sink;
        }
    }
}
