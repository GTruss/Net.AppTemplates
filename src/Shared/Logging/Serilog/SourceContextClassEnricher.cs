using Microsoft.EntityFrameworkCore.Metadata.Internal;

using Serilog.Core;
using Serilog.Events;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace My.Shared.Logging.Serilog;

public class SourceContextClassEnricher : ILogEventEnricher {
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) {
        var context = logEvent.Properties.GetValueOrDefault("SourceContext");
        if (context is not null) {
            var typeName = context.ToString();
            var pos = typeName.LastIndexOf('.');
            typeName = typeName.Substring(pos + 1, typeName.Length - pos - 2);
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("SourceContext", typeName));
        }
        else {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("SourceContext", "undefined"));
        }
    }
}
