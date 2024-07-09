
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GardenSage.Common.MeteoJson;

/// <summary>
/// A MeteoJson Weather Client which reads from the ctor file path, NOT from the live service
/// </summary>
/// <param name="datapath"></param>
public class LocalJsonClient(
        ILogger<LocalJsonClient> logger,
        IOptions<LocalJsonClient.LocalClientOptions> options)
    : ISageWeatherClient
{
    public sealed class LocalClientOptions
    {
        [Required]
        [MinLength(1)]
        public required string LocalClientPath { get; set; } = null!;
    }

    public string Datapath => options.Value.LocalClientPath;

    public async Task<IForecastDataAdapter> GetWeatherAsync(ForecastParameters p)
    {
        using (logger.BeginScope("localjsonclient_getweatherasync"))
        {
            if (!File.Exists(Datapath))
            {
                logger.LogError("filenotfound {file}", Datapath);
            }
            var json = await File.ReadAllTextAsync(Datapath);
            logger.LogInformation("Fetched {jsonchars} from fullpath: {path}", json.Length, Path.GetFullPath(Datapath));
            var data = JsonSerializer.Deserialize<JsonForecastData>(json, JsonForecastData.JsonOptions)
                ?? throw new InvalidOperationException(json);

            return new MeteoJsonAdapter(data)
            {
                Retrieved = new DateTimeOffset(data
                    .Daily.ResolveEnumerable<DateTime>("time")
                    .Skip(2).First(),
                    offset: TimeSpan.FromSeconds(data.UtcOffsetSeconds)),
            };
        }
    }
}
