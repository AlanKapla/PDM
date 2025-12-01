namespace Business.Interfaces.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(Business.Interfaces.DTO.EmailMessageDto message, CancellationToken cancellationToken = default);
    }
}