using App.Core.ProjectAggregate;
using App.SharedKernel;

namespace App.Core.ProjectAggregate.Events {
    public class ToDoItemCompletedEvent : BaseDomainEvent {
        public ToDoItem CompletedItem { get; set; }

        public ToDoItemCompletedEvent(ToDoItem completedItem) {
            CompletedItem = completedItem;
        }
    }
}