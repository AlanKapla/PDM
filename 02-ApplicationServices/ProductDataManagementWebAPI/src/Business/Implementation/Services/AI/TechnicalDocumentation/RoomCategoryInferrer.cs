namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static class RoomCategoryInferrer
{
    public static string? Infer(string? roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
        {
            return null;
        }

        string normalized = roomName.Trim().ToLowerInvariant()
            .Replace('ł', 'l')
            .Replace('ó', 'o')
            .Replace('ą', 'a')
            .Replace('ę', 'e')
            .Replace('ś', 's')
            .Replace('ć', 'c')
            .Replace('ń', 'n')
            .Replace('ź', 'z')
            .Replace('ż', 'z');

        if (ContainsAny(normalized, "wiatrolap", "przedpokoj", "komunikacja", "klatka schodowa", "hol", "korytarz"))
        {
            return "komunikacja";
        }

        if (ContainsAny(normalized, "wc", "lazienka", "łazienka"))
        {
            return "sanitarne";
        }

        if (ContainsAny(normalized, "kuchnia", "spizarnia", "spiżarnia", "garderoba", "pralnia", "pom. techn", "pomieszczenie techniczne"))
        {
            return "usługowe";
        }

        if (ContainsAny(normalized, "salon", "sypialnia", "pokoj", "pokój", "jadalnia", "gabinet"))
        {
            return "mieszkalne";
        }

        if (ContainsAny(normalized, "garaz", "garaż", "kotlownia", "kotłownia", "pom. gosp", "gospodarcze", "warsztat"))
        {
            return "gospodarcze";
        }

        return null;
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        foreach (string value in values)
        {
            if (text.Contains(value, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
