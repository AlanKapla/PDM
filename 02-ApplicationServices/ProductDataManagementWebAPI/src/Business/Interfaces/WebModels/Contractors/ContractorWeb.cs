namespace Business.Interfaces.WebModels.Contractors
{
    public sealed class ContractorWeb
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = default!;
        public string? TaxId { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
