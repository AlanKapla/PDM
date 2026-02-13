namespace Business.Interfaces.Configurations
{
    public sealed class FileShareConsolidationSettings
    {
        public const string SectionName = "FileShareConsolidation";

        /// <summary>
        /// Próg konsolidacji udostępnień (wartość od 0.0 do 1.0)
        /// Jeśli użytkownik ma >= tego % plików z paczki udostępnionych,
        /// system konsoliduje udostępnienia na poziom paczki z wykluczeniami.
        /// Domyślnie: 0.6 (60%)
        /// </summary>
        public double ConsolidationThreshold { get; set; } = 0.6;
    }
}
