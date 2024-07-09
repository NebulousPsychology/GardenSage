
using System.Text.Json;

using GardenSage.Common.MeteoJson;

namespace GardenSage.Test;
public class FlatDataTest(Xunit.Abstractions.ITestOutputHelper log)
{
    /// <summary>
    /// a subset of the fetched sample, just the part that should deserialize to FlatDataMap
    /// </summary>
    const string SAMPLE_PATH = "data/flatdata.json";

    [InlineData("precipitation_probability_mean", typeof(int))]
    [InlineData("temperature_2m_max", typeof(decimal))]
    [InlineData("sunrise", typeof(DateTime))]
    [Theory]
    public void ResolveTest(string key, Type type)
    {
        // Given
        string json = File.ReadAllText(SAMPLE_PATH);
        // When
        var data = JsonSerializer.Deserialize<FlatDataMap>(json);

        Assert.NotNull(data);
        Assert.Equal(8, data.PropertyNames.Count());
        Assert.Contains("time", data.PropertyNames);
        Assert.Contains(key, data.PropertyNames);
        // Then
        // sunrise/sunset are given in localtime?
        var invoker = typeof(FlatDataMap)
            ?.GetMethod(nameof(FlatDataMap.ResolveArray))
            ?.MakeGenericMethod(type) ??
            throw new NullReferenceException($"failed reflecting to {nameof(FlatDataMap.ResolveArray)}");

        var result = invoker.Invoke(data, [key]);
        Assert.IsAssignableFrom<Array>(result);
        // Assert.All(collection: result as Array, action: o=> Assert.IsType(type, o));    
        log.WriteLine("arrived at end");
    }
}
