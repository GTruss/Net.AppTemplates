using Serilog.Core;
using Serilog.Events;

using System.Collections.Generic;

namespace My.Shared.Logging.Serilog {
    // implement serilog enricher
    public class ApplicationNameColumnEnricher : ILogEventEnricher {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) {
            // add custom properties
            var appName = logEvent.Properties.GetValueOrDefault("ApplicationName");
            if (appName is not null) {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("AppName", appName.ToString().Replace("\"", "")));
            }
        }
    }
}
