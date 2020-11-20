using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;

namespace Net5.Common {
    public static class SerilogConfig {
        public static void Configure(IConfiguration config) {
            string logFileName = AppDomain.CurrentDomain.BaseDirectory + @$"\logs\LogFile_{ DateTime.Now:yyyyMMdd_hhmmss}.log";
            Log.Logger = new LoggerConfiguration()
                            .ReadFrom.Configuration(config)
                            .Enrich.FromLogContext().Enrich.WithMachineName()
                            .WriteTo.Console()
                            .WriteTo.File(logFileName, outputTemplate: "[{Timestamp:u} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                            .WriteTo.File(new JsonFormatter(), logFileName + ".json")
                            .CreateLogger();

            
        }
    }
}
