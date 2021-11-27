using System;
using System.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Formatting;
using System.Globalization;

namespace App.SharedKernel.Logging.Serilog {
    public class InMemorySink : ILogEventSink {
        private readonly string _outputTemplate;

        public LogEventQueue<string> Events { get; } = new LogEventQueue<string>();

        public InMemorySink(string outputTemplate) {
            _outputTemplate = outputTemplate;
        }

        public void Emit(LogEvent logEvent) {
            ITextFormatter _textFormatter = new MessageTemplateTextFormatter(_outputTemplate, CultureInfo.InvariantCulture);
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            var renderSpace = new StringWriter();
            _textFormatter.Format(logEvent, renderSpace);
            Events.Enqueue(renderSpace.ToString());
        }
    }
}
