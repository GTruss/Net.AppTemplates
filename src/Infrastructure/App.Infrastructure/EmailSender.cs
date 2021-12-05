using App.Services.Interfaces;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Serilog.Context;

using System.Net.Mail;
using System.Threading.Tasks;

namespace App.Infrastructure {
    public class EmailSender : IEmailSender {
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(ILogger<EmailSender> logger) {
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string from, string subject, string body) {
            var emailClient = new SmtpClient("localhost");
            var message = new MailMessage {
                From = new MailAddress(from),
                Subject = subject,
                Body = body
            };
            message.To.Add(new MailAddress(to));

            using (LogContext.PushProperty("Message", JsonConvert.SerializeObject(message))) {
                _logger.LogWarning("Sending email:\n {subject}", subject);
            }

            await emailClient.SendMailAsync(message);
        }
    }
}
