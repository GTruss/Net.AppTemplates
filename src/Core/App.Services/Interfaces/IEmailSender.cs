using System.Threading.Tasks;

namespace App.Services.Interfaces;

public interface IEmailSender {
    Task SendEmailAsync(string to, string from, string subject, string body);
}
