namespace Business.Interfaces.DTO
{
    /// <summary>
    /// Web model reprezentujący plik projektu
    /// </summary>
    public class ProjectFileDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string PackageName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
        public Guid UploadedByUserId { get; set; }
    }
}
