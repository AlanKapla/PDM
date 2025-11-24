namespace Business.Interfaces.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
    }

    public sealed class EmailMessage
    {
        /// <summary>
        /// Adres nadawcy. Jeśli null – użyj z konfiguracji.
        /// </summary>
        public string? From { get; set; }

        public string To { get; set; } = default!;
        public string Subject { get; set; } = default!;

        /// <summary>
        /// Treść tekstowa (plain text). Opcjonalna, ale warto ją mieć.
        /// </summary>
        public string? TextBody { get; set; }

        /// <summary>
        /// Treść HTML. Jeśli podasz tylko HTML – też ok.
        /// </summary>
        public string? HtmlBody { get; set; }
    }
}