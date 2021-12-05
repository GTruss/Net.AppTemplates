using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace My.Shared.Logging.Serilog {
    public class LogEventQueue<T> {
        private readonly Queue<T> queue = new Queue<T>();
        public event EventHandler<LogEventQueueArgs> Enqueued;

        protected virtual void OnEnqueued(T item) {
            if (Enqueued != null)
                Enqueued(this, new LogEventQueueArgs() { Message = item.ToString() });
        }

        public virtual void Enqueue(T item) {
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
    }
}
