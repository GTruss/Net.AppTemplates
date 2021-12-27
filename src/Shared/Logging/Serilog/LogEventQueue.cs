using Serilog.Events;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace My.Shared.Logging.Serilog {
    public class LogEventQueue {
        private readonly Queue<EventQueueMessage> queue = new Queue<EventQueueMessage>();
        public event EventHandler<LogEventQueueArgs> Enqueued;

        protected virtual void OnEnqueued(EventQueueMessage item) {
            if (Enqueued != null) {
                Enqueued(this, new LogEventQueueArgs() { 
                    Message = item.Message, 
                    LogEvent = item.LogEvent
                });
            }
        }

        public virtual void Enqueue(EventQueueMessage item) {
            queue.Enqueue(item);
            OnEnqueued(item);
        }

        public int Count {
            get {
                return queue.Count;
            }
        }
    }

    public class LogEventQueueArgs : EventArgs {
        public string Message { get; set; }
        public LogEvent LogEvent { get; set; }
    }
}
