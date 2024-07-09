
using System.Text.Json;

using GardenSage.Common.MeteoJson;
using GardenSage.Common.Options;

using Xunit.Abstractions;

namespace GardenSage.Test;
public class LocalDeserializeTests(ITestOutputHelper log)
{
    /// <summary>
    /// fetch.json is a sample retrieved from the service
    /// </summary>
    public const string FULL_DOCUMENT_PATH = "data/fetch_240707.json";

    /// <summary>
    /// Test that a non-empty file exists at <paramref name="filepath"/>
    /// </summary>
    [Theory]
    [InlineData(FULL_DOCUMENT_PATH)]
    public void HasPrefetchedData(string filepath)
    {
        Assert.True(File.Exists(filepath));
        string text = File.ReadAllText(filepath);
        log.WriteLine(text);
        Assert.NotNull(text);
        Assert.NotEmpty(text);
    }

    /// <summary>
    /// Ensures that deserialization occurs, and that sample values line up
    /// </summary>
    [Fact]
    public void DeserializesJsonToForecastData()
    {
        // Given
        string json = File.ReadAllText(FULL_DOCUMENT_PATH);
        // When
        var data = JsonSerializer.Deserialize<JsonForecastData>(json, JsonForecastData.JsonOptions);
        Assert.NotNull(data);

        // Then
        Assert.Equal(SageOptions.DEFAULT_LATITUDE, data.Latitude, precision: 5);
        Assert.Equal(0.1779794692993164, data.GenerationTimeMs);
        Assert.Equal(6, data.HourlyUnits.Count);
        // Assert.Equal(5, data.Hourly.Count);
    }
}
