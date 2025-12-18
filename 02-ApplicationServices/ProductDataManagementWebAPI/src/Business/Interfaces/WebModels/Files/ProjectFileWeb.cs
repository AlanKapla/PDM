namespace Business.Interfaces.WebModels.Files
{
    /// <summary>
    /// Web model representing project file returned by API
    /// </summary>
    public record ProjectFileWeb
    {
        public Guid Id { get; init; }
        public string FileName { get; init; } = default!;
        public string DisplayName { get; init; } = default!;
        public string PackageName { get; init; } = default!;
        public DateTime CreatedAt { get; init; }
        public Guid OwnerId { get; init; }
        public string OwnerName { get; init; } = default!;
        
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
        public int TotalVersions { get; init; }
        
        /// <summary>
        /// Czy użytkownik jest właścicielem pliku
        /// </summary>
        public bool IsOwner { get; init; }
        
        /// <summary>
        /// Czy plik został udostępniony obecnemu użytkownikowi
        /// </summary>
        public bool IsShared { get; init; }
        
        /// <summary>
        /// Lista ID użytkowników, którym udostępniono ten plik
        /// </summary>
        public List<Guid> SharedWithUserIds { get; init; } = new();
    }
}
