using App.Services.Events;
using App.Services.Interfaces;

using MediatR;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace App.Services.Handlers;

public class SomeServiceNotificationHandler : INotificationHandler<SomeServiceEvent> {
    private readonly IEmailSender _emailSender;

    public SomeServiceNotificationHandler(IEmailSender emailSender) {
        _emailSender = emailSender;
    }

    public Task Handle(SomeServiceEvent notification, CancellationToken cancellationToken) {
        return _emailSender.SendEmailAsync(
            "test@test.com", 
            "test@test.com", 
            $"{notification.Message}", 
            $"{notification.Message}\n\n{notification.DateOccurred:G}");
    }
}
