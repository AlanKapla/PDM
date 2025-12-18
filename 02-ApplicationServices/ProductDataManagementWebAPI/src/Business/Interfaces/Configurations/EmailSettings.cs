namespace Business.Interfaces.Configurations
{
    public sealed class EmailSettings
    {
        public const string SectionName = "EmailSettings";
        public string Provider { get; set; } = "SendGrid";
        public SendGridSettings SendGrid { get; set; } = new();
    }

    public sealed class SendGridSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string DefaultFromEmail { get; set; } = string.Empty;
        public string DefaultFromName { get; set; } = "System";
    }
}
