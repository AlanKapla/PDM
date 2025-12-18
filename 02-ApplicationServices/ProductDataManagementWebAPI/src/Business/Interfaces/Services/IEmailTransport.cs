using System.Threading;
using System.Threading.Tasks;
using Business.Interfaces.DTO;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Low-level email transport responsible for actually sending emails.
    /// Used by background workers. Application code should use IEmailSender which enqueues messages.
    /// </summary>
    public interface IEmailTransport
    {
        Task SendEmailAsync(EmailMessageDto message, CancellationToken cancellationToken = default);
    }
}
