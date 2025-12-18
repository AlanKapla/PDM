namespace Business.Interfaces.DTO
{
    public sealed class EmailMessageDto
    {
        public string? From { get; set; }
        public string To { get; set; } = default!;
        public string Subject { get; set; } = default!;
        public string? TextBody { get; set; }
        public string? HtmlBody { get; set; }
    }
}
