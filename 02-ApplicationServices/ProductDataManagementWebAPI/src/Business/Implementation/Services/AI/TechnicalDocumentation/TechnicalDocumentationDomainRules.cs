namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class TechnicalDocumentationDomainRules
{
    public const string ExtractionUserPrefix = """
        REGUŁY DOMENOWE (obowiązkowe, budownictwo PL):

        POMIESZCZENIA
        - Jeden wpis na unikalne pomieszczenie (symbol lub nazwa) NA KONDYGNACJI. NIE duplikuj Garaż/Wiatrołap/Kuchnia na tym samym rzucie.
        - Jeśli rysunek podaje powierzchnię wprost (np. 21,3 m²) → użyj jako areaM2, NIE przeliczaj z wymiarów.
        - Jeśli brak podanej powierzchni → areaM2 = widthM × lengthM (zaokrąglij do 0,1 m²).
        - Powierzchnia tylko z konturu pomieszczenia na TYM rzucie, nie sumuj sąsiednich.

        ŚCIANY — layers[] TYLKO warstwy konstrukcyjne ściany
        - DOZWOLONE: beton komórkowy (Ytong/Porotherm/H+H), silikat, pustak ceramiczny/keramzytowy, żelbet, tynk, zaprawa murarska, styropian/XPS/grafit/wełna w warstwie ściany.
        - ZABRONIONE w layers ścian: dachówka, kontrłaty, krokwie, płatwie, stężenia, blacha, rynny, stelaże okienne, kratownice — to idzie do roof/timber/openings, NIE do walls.

        OTWORY (okna/drzwi)
        - count = liczba sztuk TEGO typu na TYM rzucie (nie mnoż przez liczbę stron PDF).
        - Grupuj po symbolu (O1, D1). Ten sam symbol = jeden wpis z właściwym count.
        - widthCm/heightCm w cm, typowo okno 60–300 cm, drzwi 70–120 × 200–240 cm.

        FUNDAMENTY
        - blocks = bloczki fundamentowe betonowe (Fb), NIE beton komórkowy ścianowy.
        - Beton komórkowy Ytong/Porotherm/H+H → wyłącznie ściany (walls.layers), NIE foundations.blocks.
        - concrete = beton w ławach/stopach (klasa C20/25, C25/30) w m³.
        - steel = stal zbrojeniowa ław: format Ø12, Ø16 (średnica pręta). NIE używaj Q — Q to siatki zbrojeniowe (stropy).
        - insulation = styropian/XPS podposadzkowy, izolacja termiczna fundamentu w m².

        STROPY
        - reinforcement w slabs: siatka Q188, Q335 lub pręty Ø — rozróżniaj: Q=siatka, Ø=pręt.

        DACH (sekcja roof)
        - coveringType = pokrycie (dachówka, blachodachówka).
        - timber = krokwie, kontrłaty, murłaty, płatwie (NIE w walls).
        - Przekrój drewna na rysunku: 20x5 lub 20/5 (cm) — w JSON zapisz jako section: "20x5".

        OPIS SŁOWNY I TABELA RYSUNKOWA (OBOWIĄZKOWE)
        - Zawsze odczytaj opis słowny i tabelę/legendę rysunkową — to główne źródło symboli Z1/O1/D1 i materiałów.
        - Dla każdego symbolu z tabeli projektowej utwórz wpis w walls[] lub openings[].
        - Dane z opisu/tabeli uzupełniają geometrię (layers, steel, reinforcement, concreteClass).
        - Szczegół na innym arkuszu → deferredDetails (nie zgaduj). crossReferences = jawne odsyłacze z rysunku.
        - Jeśli w kontekście jest KATALOG — użyj opisów/tabel powiązanych arkuszy do uzupełnienia danych.

        JEDNOSTKI MIAR (obowiązkowe)
        - bloczki/pustaki/cegły → szt
        - beton (ławy, stropy, jądro ściany) → m3
        - stal zbrojeniowa, siatki Q, pręty Ø → kg
        - styropian/XPS/wełna/tynk → m2
        - drewno konstrukcyjne (krokwie, murłaty, kontrłaty, płatwie) → m3 (podaj section i lengthM do obliczenia objętości)
        - dachówka, pokrycie dachu → m2

        """;

    public static bool IsWallLayerMaterial(string material)
    {
        if (string.IsNullOrWhiteSpace(material))
        {
            return false;
        }

        string normalized = Normalize(material);

        if (IsExcludedWallMaterial(normalized))
        {
            return false;
        }

        return ContainsAny(normalized,
            "beton komórkowy", "ytong", "porotherm", "h+h", "silikat", "pustak", "keramzyt",
            "ceramicz", "żelbet", "beton", "tynk", "zaprawa", "murarsk",
            "styropian", "xps", "grafit", "wełna", "mineraln", "izolac");
    }

    public static bool IsFoundationBlockMaterial(string material)
    {
        string normalized = Normalize(material);
        if (ContainsAny(normalized, "ytong", "porotherm", "komórkow", "komorkow", "h+h"))
        {
            return false;
        }

        return ContainsAny(normalized, "fundament", "fb", "bloczek", "betonowy");
    }

    public static bool IsThermalInsulationMaterial(string material)
    {
        string normalized = Normalize(material);
        return ContainsAny(normalized, "styropian", "xps", "eps", "grafit", "wełna", "mineraln", "izolac term");
    }

    public static string NormalizeRoomName(string name)
    {
        return name.Trim().ToLowerInvariant()
            .Replace("ł", "l")
            .Replace("ó", "o")
            .Replace("ą", "a")
            .Replace("ę", "e")
            .Replace("ś", "s")
            .Replace("ć", "c")
            .Replace("ń", "n")
            .Replace("ź", "z")
            .Replace("ż", "z");
    }

    private static bool IsExcludedWallMaterial(string normalized)
    {
        return ContainsAny(normalized,
            "dachówk", "dachowk", "kontrłat", "kontrlat", "krokw", "płatwi", "platwi",
            "stężen", "stezen", "stelaz", "stalow", "blach", "rynna", "rynien",
            "okładzin", "okladzin", "pokrycie dach");
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static bool ContainsAny(string text, params string[] tokens)
    {
        foreach (string token in tokens)
        {
            if (text.Contains(token, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
