namespace Business.Interfaces.Configurations;

public sealed class TechnicalDocumentationOptions
{
    public const string SectionName = "TechnicalDocumentation";

    private const long DefaultCompressionThresholdBytes = 3_145_728;

    private const int DefaultMaxImagesPerGroup = 6;

    /// <summary>
    /// Włącza krok testowy porównania gotowego modelu ze wzorcem (details_schema_reference.json).
    /// Domyślnie wyłączone — tylko do testów / debugowania pipeline'u.
    /// </summary>
    public bool EnableTestValidation { get; set; }

    /// <summary>
    /// Po deterministycznym diffie uruchamia agenta AI (wyjaśnienia + weryfikacja na obrazach).
    /// Wymaga EnableTestValidation = true.
    /// </summary>
    public bool EnableTestValidationAiReview { get; set; }

    /// <summary>
    /// Przełącza pipeline grup tematycznych (9 faz) zamiast legacy per-drawing.
    /// Domyślnie wyłączone w produkcji.
    /// </summary>
    public bool UseGroupPipeline { get; set; }

    /// <summary>
    /// Maksymalna liczba obrazów w jednym call ekstrakcji per grupa.
    /// Przy przekroczeniu grupa jest dzielona na sub-batch'e (merge w C# przed weryfikacją).
    /// </summary>
    public int MaxImagesPerGroup { get; set; } = DefaultMaxImagesPerGroup;

    /// <summary>
    /// Próg rozmiaru pliku (bajty), powyżej którego obraz jest kompresowany przed wysłaniem do vision API.
    /// Domyślnie 3 MB (limit inline base64 w Azure OpenAI).
    /// </summary>
    public long CompressionThresholdBytes { get; set; } = DefaultCompressionThresholdBytes;

    /// <summary>
    /// Mapowanie <c>drawingType</c> → nazwy grup tematycznych (np. K-06 → reinforcement + foundations).
    /// Puste lub brak klucza w konfiguracji — używane są wartości domyślne z <see cref="CreateDefaultDrawingTypeToThematicGroups"/>.
    /// </summary>
    public Dictionary<string, string[]> DrawingTypeToThematicGroups { get; set; } = [];

    public IReadOnlyDictionary<string, string[]> GetEffectiveDrawingTypeToThematicGroups()
    {
        if (DrawingTypeToThematicGroups.Count > 0)
        {
            return DrawingTypeToThematicGroups;
        }

        return CreateDefaultDrawingTypeToThematicGroups();
    }

    public static Dictionary<string, string[]> CreateDefaultDrawingTypeToThematicGroups()
    {
        return new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [DrawingTypes.ZbrojenieStropuDolne] = [ThematicGroups.Reinforcement],
            [DrawingTypes.ZbrojenieStropuGorne] = [ThematicGroups.Reinforcement],
            [DrawingTypes.DetaleKonstrukcyjne] = [ThematicGroups.Reinforcement, ThematicGroups.Foundations],
            [DrawingTypes.RzutWiezbyDachowej] = [ThematicGroups.RoofStructure],
            [DrawingTypes.AksonometriaWiezby] = [ThematicGroups.RoofStructure],
            [DrawingTypes.RzutDachu] = [ThematicGroups.RoofStructure, ThematicGroups.FloorPlans],
            [DrawingTypes.RzutParteru] = [ThematicGroups.FloorPlans],
            [DrawingTypes.RzutPietra] = [ThematicGroups.FloorPlans],
            [DrawingTypes.RzutPoddasza] = [ThematicGroups.FloorPlans],
            [DrawingTypes.RzutPiwnicy] = [ThematicGroups.FloorPlans],
            [DrawingTypes.Przekroj] = [ThematicGroups.Sections],
            [DrawingTypes.Elewacja] = [ThematicGroups.Elevations],
            [DrawingTypes.RzutFundamentow] = [ThematicGroups.Foundations],
            [DrawingTypes.ZagospodarowanieTerenu] = [ThematicGroups.Site],
            [DrawingTypes.OpisTechniczny] = [ThematicGroups.Other],
            [DrawingTypes.Nieznany] = [ThematicGroups.Other],
        };
    }

    public static class ThematicGroups
    {
        public const string Reinforcement = "reinforcement";

        public const string RoofStructure = "roof_structure";

        public const string FloorPlans = "floor_plans";

        public const string Sections = "sections";

        public const string Elevations = "elevations";

        public const string Foundations = "foundations";

        public const string Site = "site";

        public const string Other = "other";
    }

    public static class DrawingTypes
    {
        public const string ZbrojenieStropuDolne = "zbrojenie_stropu_dolne";

        public const string ZbrojenieStropuGorne = "zbrojenie_stropu_gorne";

        public const string DetaleKonstrukcyjne = "detale_konstrukcyjne";

        public const string RzutWiezbyDachowej = "rzut_wiezby_dachowej";

        public const string AksonometriaWiezby = "aksonometria_wiezby";

        public const string RzutDachu = "rzut_dachu";

        public const string RzutParteru = "rzut_parteru";

        public const string RzutPietra = "rzut_piętra";

        public const string RzutPoddasza = "rzut_poddasza";

        public const string RzutPiwnicy = "rzut_piwnicy";

        public const string Przekroj = "przekroj";

        public const string Elewacja = "elewacja";

        public const string RzutFundamentow = "rzut_fundamentow";

        public const string ZagospodarowanieTerenu = "zagospodarowanie_terenu";

        public const string OpisTechniczny = "opis_techniczny";

        public const string Nieznany = "nieznany";
    }
}
