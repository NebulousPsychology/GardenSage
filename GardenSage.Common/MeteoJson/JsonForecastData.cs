
using System.Text.Json.Serialization;

namespace GardenSage.Common.MeteoJson;

public class JsonForecastData
{
    public float Latitude { get; init; }
    public float Longitude { get; init; }
    [JsonPropertyName("generationtime_ms")]
    public float GenerationTimeMs { get; init; }
    public int UtcOffsetSeconds { get; init; }
    public required string Timezone { get; init; }
    public required string TimezoneAbbreviation { get; init; }
    public float Elevation { get; init; }
    public Dictionary<string, string> HourlyUnits { get; init; } = [];
    public required FlatDataMap Hourly { get; init; } = new() { RawDataMembers = [] };
    public Dictionary<string, string> DailyUnits { get; init; } = [];
    public required FlatDataMap Daily { get; init; } = new() { RawDataMembers = [] };

    public static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
    };

}
