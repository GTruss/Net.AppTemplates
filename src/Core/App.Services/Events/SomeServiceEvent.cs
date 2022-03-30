using App.SharedKernel;

using Microsoft.Extensions.Logging;

namespace App.Services.Events;

public class SomeServiceEvent : BaseDomainEvent {
    public readonly string Message;

    public SomeServiceEvent(string message) { 
        Message = message;
    }
}
