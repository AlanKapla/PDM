namespace Business.Interfaces.WebModels.Files
{
    /// <summary>
    /// Web model reprezentujący plik udostępniony użytkownikowi
    /// </summary>
    public sealed record SharedProjectFileWeb
    {
        /// <summary>
        /// ID wpisu udostępnienia
        /// </summary>
        public required Guid Id { get; init; }

        /// <summary>
        /// ID oryginalnego pliku
        /// </summary>
        public required Guid ProjectFileId { get; init; }

        public required string FileName { get; init; }
        public required string DisplayName { get; init; }
        public required string PackageName { get; init; }
        public required string ContentType { get; init; }
        public required long FileSizeBytes { get; init; }

        /// <summary>
        /// Data przesłania oryginalnego pliku
        /// </summary>
        public required DateTime UploadedAt { get; init; }

        /// <summary>
        /// Data udostępnienia pliku
        /// </summary>
        public required DateTime SharedAt { get; init; }

        /// <summary>
        /// ID użytkownika, który udostępnił plik
        /// </summary>
        public required Guid SharedByUserId { get; init; }

        /// <summary>
        /// Nazwa użytkownika, który udostępnił plik
        /// </summary>
        public required string SharedByUserName { get; init; }

        /// <summary>
        /// ID oryginalnego właściciela pliku
        /// </summary>
        public required Guid OriginalOwnerUserId { get; init; }

        /// <summary>
        /// Nazwa oryginalnego właściciela pliku
        /// </summary>
        public required string OriginalOwnerUserName { get; init; }

        /// <summary>
        /// Temporary URL with SAS token for direct file access
        /// </summary>
        public required string SasUrl { get; init; }

        /// <summary>
        /// Aktywna wersja pliku
        /// </summary>
        public ProjectFileVersionWeb? CurrentVersion { get; init; }

        /// <summary>
        /// Wszystkie wersje pliku wraz z komentarzami
        /// </summary>
        public List<ProjectFileVersionWeb> Versions { get; init; } = new();

        /// <summary>
        /// Całkowita liczba wersji pliku
        /// </summary>
        public required int TotalVersions { get; init; }
    }
}
