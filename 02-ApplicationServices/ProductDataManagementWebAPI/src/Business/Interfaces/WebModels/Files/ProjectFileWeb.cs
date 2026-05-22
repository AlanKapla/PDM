namespace Business.Interfaces.WebModels.Files
{
    /// <summary>
    /// Web model representing project file returned by API
    /// </summary>
    public sealed record ProjectFileWeb
    {
        public required Guid Id { get; init; }
        public required string FileName { get; init; }
        public required string DisplayName { get; init; }
        public required string PackageName { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required Guid OwnerId { get; init; }
        public required string OwnerName { get; init; }

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

        /// <summary>
        /// Czy użytkownik jest właścicielem pliku
        /// </summary>
        public required bool IsOwner { get; init; }

        /// <summary>
        /// Czy plik został udostępniony obecnemu użytkownikowi
        /// </summary>
        public required bool IsShared { get; init; }

        /// <summary>
        /// Lista ID użytkowników, którym udostępniono ten plik
        /// </summary>
        public List<Guid> SharedWithUserIds { get; init; } = new();
    }
}
