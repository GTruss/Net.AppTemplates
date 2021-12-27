using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Formatting;
using System.Globalization;
using Newtonsoft.Json;

namespace My.Shared.Logging.Serilog {
    public class InMemorySink : ILogEventSink {
        private readonly string _outputTemplate;
        public LogEventQueue Events { get; } = new LogEventQueue();

        public InMemorySink(string outputTemplate) {
            _outputTemplate = outputTemplate;
        }

        public void Emit(LogEvent logEvent) {
            ITextFormatter _textFormatter = new MessageTemplateTextFormatter(_outputTemplate, CultureInfo.InvariantCulture);
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            var renderSpace = new StringWriter();
            _textFormatter.Format(logEvent, renderSpace);

            string render = renderSpace.ToString();
            Events.Enqueue(new() {
                Message = render,
                LogEvent = logEvent
            });
        }
    }

    public record EventQueueMessage() {
        public string Message {get; init; }
        public LogEvent LogEvent { get; init; }
    }
}
