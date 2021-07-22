using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;
using Serilog.Sinks.MSSqlServer;

namespace Net5.Cli {
    public static class SerilogConfig {
        public static void Configure(IConfiguration config) {
            string logFileName = AppDomain.CurrentDomain.BaseDirectory + @$"\logs\LogFile_{ DateTime.Now:yyyyMMdd_hhmmss}.log";
            Log.Logger = new LoggerConfiguration()
                            .ReadFrom.Configuration(config)
                            .Enrich.FromLogContext()
                            .Enrich.WithMachineName()
                            .Enrich.With<EventTypeEnricher>()
                            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} ({EventType}) {Level:u3}] {Message:lj}{NewLine}{Exception}")
                            .WriteTo.File(logFileName, outputTemplate: "[{Timestamp:HH:mm:ss.fff} ({EventType}) {Level:u3}] {Message:lj}{NewLine}{Exception}")
                            .WriteTo.File(new JsonFormatter(), logFileName + ".json")
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
        }
    }
}
