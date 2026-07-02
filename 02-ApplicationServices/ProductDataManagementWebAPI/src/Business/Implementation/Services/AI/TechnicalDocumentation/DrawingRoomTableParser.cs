using System.Globalization;
using System.Text.RegularExpressions;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;

namespace Business.Implementation.Services.AI.TechnicalDocumentation;

internal static partial class DrawingRoomTableParser
{
    public static List<Room> Parse(string tableContent)
    {
        List<Room> rooms = new();
        string[] segments = tableContent.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string segment in segments)
        {
            Match match = RoomRowRegex().Match(segment);
            if (!match.Success)
            {
                continue;
            }

            string symbol = match.Groups[1].Value.Trim();
            string name = match.Groups[2].Value.Trim();
            string areaText = match.Groups[3].Value.Replace(',', '.');
            if (!double.TryParse(areaText, NumberStyles.Float, CultureInfo.InvariantCulture, out double areaM2))
            {
                continue;
            }

            rooms.Add(new Room
            {
                Symbol = symbol,
                Number = symbol,
                Name = name,
                AreaM2 = areaM2
            });
        }

        return rooms;
    }

    [GeneratedRegex(@"^(\d+)-([^-]+)-([\d.,]+)\s*m2$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RoomRowRegex();
}
