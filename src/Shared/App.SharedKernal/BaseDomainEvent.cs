using MediatR;

using System;

namespace App.SharedKernel;

public abstract class BaseDomainEvent : INotification {
    public DateTime DateOccurred { get; protected set; } = DateTime.UtcNow;
}
