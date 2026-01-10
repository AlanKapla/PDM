using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Business.AIAgent.Interfaces;
using Business.AIAgent.Models;
using Microsoft.Extensions.Logging;

namespace Business.AIAgent.Tools;

/// <summary>
/// Example tool that gets current date and time
/// Demonstrates how to implement ITool interface
/// </summary>
public sealed class GetCurrentDateTimeTool : ToolBase
{
    private readonly ILogger<GetCurrentDateTimeTool> logger;

    public GetCurrentDateTimeTool(ILogger<GetCurrentDateTimeTool> logger)
    {
        this.logger = logger;
    }

    public override string Name => "get_current_datetime";

    public override string Description => 
        "Gets the current date and time. Can return in different timezones and formats.";

    public override object GetParametersSchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                timezone = new
                {
                    type = "string",
                    description = "Timezone (e.g., 'UTC', 'Europe/Warsaw', 'America/New_York'). Default is UTC.",
                    @enum = new[] { "UTC", "Europe/Warsaw", "America/New_York", "Asia/Tokyo" }
                },
                format = new
                {
                    type = "string",
                    description = "Date format (e.g., 'ISO8601', 'Short', 'Long'). Default is ISO8601.",
                    @enum = new[] { "ISO8601", "Short", "Long", "Custom" }
                },
                customFormat = new
                {
                    type = "string",
                    description = "Custom date format string (used only when format='Custom'). Example: 'yyyy-MM-dd HH:mm:ss'"
                }
            },
            required = Array.Empty<string>() // All parameters are optional
        };
    }

    public override Task<ToolResult> ExecuteAsync(string arguments, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Parse arguments
            var args = JsonSerializer.Deserialize<GetCurrentDateTimeArgs>(arguments);
            if (args == null)
            {
                return Task.FromResult(ToolResult.Failure(
                    string.Empty,
                    Name,
                    "Failed to parse arguments",
                    stopwatch.ElapsedMilliseconds));
            }

            logger.LogDebug("Getting current date/time for timezone: {Timezone}, format: {Format}",
                args.Timezone ?? "UTC", args.Format ?? "ISO8601");

            // Get current time in specified timezone
            TimeZoneInfo timeZone = GetTimeZone(args.Timezone);
            DateTimeOffset now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);

            // Format the datetime
            string formattedDateTime = FormatDateTime(now, args.Format, args.CustomFormat);

            // Build result
            var result = new
            {
                datetime = formattedDateTime,
                timezone = timeZone.Id,
                timestamp_utc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                iso8601 = now.ToString("O")
            };

            string resultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            stopwatch.Stop();

            logger.LogInformation("Current date/time retrieved: {DateTime} ({Timezone})",
                formattedDateTime, timeZone.Id);

            return Task.FromResult(ToolResult.Success(
                string.Empty,
                Name,
                resultJson,
                stopwatch.ElapsedMilliseconds));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Error getting current date/time");

            return Task.FromResult(ToolResult.Failure(
                string.Empty,
                Name,
                $"Error: {ex.Message}",
                stopwatch.ElapsedMilliseconds));
        }
    }

    private TimeZoneInfo GetTimeZone(string? timezoneId)
    {
        if (string.IsNullOrWhiteSpace(timezoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            logger.LogWarning("Timezone {TimezoneId} not found, using UTC", timezoneId);
            return TimeZoneInfo.Utc;
        }
    }

    private string FormatDateTime(DateTimeOffset dateTime, string? format, string? customFormat)
    {
        return format?.ToUpperInvariant() switch
        {
            "SHORT" => dateTime.ToString("g"),
            "LONG" => dateTime.ToString("F"),
            "CUSTOM" when !string.IsNullOrWhiteSpace(customFormat) => dateTime.ToString(customFormat),
            "ISO8601" or _ => dateTime.ToString("O")
        };
    }

    private sealed class GetCurrentDateTimeArgs
    {
        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("customFormat")]
        public string? CustomFormat { get; set; }
    }
}
