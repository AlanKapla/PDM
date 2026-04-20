namespace Business.Interfaces.Configurations
{
    public sealed class SmtpSettings
    {
        public const string SectionName = "Email";
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SenderAddress { get; set; } = string.Empty;
        public string SenderName { get; set; } = "System";
        public string AppPassword { get; set; } = string.Empty;
    }
}
