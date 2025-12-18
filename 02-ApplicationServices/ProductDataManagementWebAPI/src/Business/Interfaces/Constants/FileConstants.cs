namespace Business.Interfaces.Constants
{
    /// <summary>
    /// Stałe związane z plikami projektu - walidacja i limity
    /// </summary>
    public static class FileConstants
    {
        /// <summary>
        /// Dozwolone rozszerzenia plików
        /// </summary>
        public static readonly string[] AllowedExtensions = 
        { 
            ".jpg", 
            ".jpeg", 
            ".pdf" 
        };

        /// <summary>
        /// Dozwolone typy MIME
        /// </summary>
        public static readonly string[] AllowedContentTypes = 
        { 
            "image/jpeg", 
            "image/jpg", 
            "application/pdf" 
        };

        /// <summary>
        /// Maksymalny rozmiar pojedynczego pliku (10 MB)
        /// </summary>
        public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        /// <summary>
        /// Maksymalna liczba plików w jednym uploadzle
        /// </summary>
        public const int MaxFilesPerUpload = 50;

        /// <summary>
        /// Maksymalna długość nazwy paczki
        /// </summary>
        public const int MaxPackageNameLength = 200;

        /// <summary>
        /// Maksymalna długość nazwy wyświetlanej pliku
        /// </summary>
        public const int MaxDisplayNameLength = 255;

        /// <summary>
        /// Maksymalna długość komentarza do wersji pliku
        /// </summary>
        public const int MaxCommentLength = 2000;

        /// <summary>
        /// Zwraca sformatowany string z dozwolonymi rozszerzeniami (dla komunikatów błędów)
        /// </summary>
        public static string GetAllowedExtensionsMessage() 
            => string.Join(", ", AllowedExtensions);

        /// <summary>
        /// Zwraca sformatowany string z dozwolonymi typami MIME (dla komunikatów błędów)
        /// </summary>
        public static string GetAllowedContentTypesMessage() 
            => string.Join(", ", AllowedContentTypes);
    }
}
