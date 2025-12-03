namespace Business.Interfaces.WebModels.Files
{
    /// <summary>
    /// Web model reprezentujący plik udostępniony użytkownikowi
    /// </summary>
    public record SharedProjectFileWeb
    {
        /// <summary>
        /// ID wpisu udostępnienia
        /// </summary>
        public Guid Id { get; init; }
        
        /// <summary>
        /// ID oryginalnego pliku
        /// </summary>
        public Guid ProjectFileId { get; init; }
        
        public string FileName { get; init; } = default!;
        public string DisplayName { get; init; } = default!;
        public string PackageName { get; init; } = default!;
        public string ContentType { get; init; } = default!;
        public long FileSizeBytes { get; init; }
        
        /// <summary>
        /// Data przesłania oryginalnego pliku
        /// </summary>
        public DateTime UploadedAt { get; init; }
        
        /// <summary>
        /// Data udostępnienia pliku
        /// </summary>
        public DateTime SharedAt { get; init; }
        
        /// <summary>
        /// ID użytkownika, który udostępnił plik
        /// </summary>
        public Guid SharedByUserId { get; init; }
        
        /// <summary>
        /// Nazwa użytkownika, który udostępnił plik
        /// </summary>
        public string SharedByUserName { get; init; } = default!;
        
        /// <summary>
        /// ID oryginalnego właściciela pliku
        /// </summary>
        public Guid OriginalOwnerUserId { get; init; }
        
        /// <summary>
        /// Nazwa oryginalnego właściciela pliku
        /// </summary>
        public string OriginalOwnerUserName { get; init; } = default!;
        
        /// <summary>
        /// Temporary URL with SAS token for direct file access
        /// </summary>
        public string SasUrl { get; init; } = default!;
    }
}
