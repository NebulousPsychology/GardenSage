using System.Collections.Immutable;


namespace GardenSage.Common.MeteoJson;

/// <summary>
/// A IForecastDataAdapter which accepts JsonForecastData
/// </summary>
public class MeteoJsonAdapter : IForecastDataAdapter
{
    private static DateTimeOffset ConvertDateTime(DateTime t, TimeSpan offset) => new(t, offset);

    private static ImmutableSortedDictionary<DateTimeOffset, T>
        LocalAdaptedData<T>(FlatDataMap source, string key, TimeSpan offset)
        => source.Resolve<DateTime, T>(key)
            .ToImmutableSortedDictionary(dtkvp => ConvertDateTime(dtkvp.Key, offset), o => o.Value);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="key"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    private static ImmutableSortedDictionary<DateTimeOffset, T>
        LocalAdaptedData<T>(FlatDataMap source, string key, int offset)
        => LocalAdaptedData<T>(source, key, TimeSpan.FromSeconds(offset));

    public MeteoJsonAdapter(JsonForecastData source)
    {
        _temp = new(() => LocalAdaptedData<float>(source.Hourly, "temperature_2m", source.UtcOffsetSeconds));
        _pc = new(LocalAdaptedData<int>(source.Hourly, "precipitation_probability", source.UtcOffsetSeconds));
        _ccp = new(LocalAdaptedData<int>(source.Hourly, "cloud_cover", source.UtcOffsetSeconds));
        _sm = new(LocalAdaptedData<float>(source.Hourly, "soil_moisture_0_to_1cm", source.UtcOffsetSeconds));
        IEnumerable<DateTime> y = source.Hourly.ResolveArray<DateTime>("time").AsEnumerable<DateTime>();
        var timeExtents = y.Aggregate<DateTime, (DateTime Min, DateTime Max)>(seed: (Min: DateTime.MaxValue, Max: DateTime.MinValue),
                func: (acc, t) => (Min: acc.Min < t ? acc.Min : t, Max: acc.Max > t ? acc.Max : t));

        Metadata = new
        {
            x = source.Daily
                .ResolveEnumerable<DateTime>("time")
                .Select(dt => ConvertDateTime(dt, TimeSpan.FromSeconds(source.UtcOffsetSeconds)))
                .Skip(2)
                .First(),
            timeExtents,
            calculatedSpan = (timeExtents.Max - timeExtents.Min).ToString(),
            dayDatacount = source.Daily.CountOf("time") ?? -1,
            hourDatacount = source.Hourly.CountOf("time") ?? -1,
        };
    }

    private readonly Lazy<ImmutableSortedDictionary<DateTimeOffset, float>> _temp;
    private readonly Lazy<ImmutableSortedDictionary<DateTimeOffset, int>> _pc;
    private readonly Lazy<ImmutableSortedDictionary<DateTimeOffset, int>> _ccp;
    private readonly Lazy<ImmutableSortedDictionary<DateTimeOffset, float>> _sm;

    public ImmutableSortedDictionary<DateTimeOffset, float> Temperature => _temp.Value;
    public ImmutableSortedDictionary<DateTimeOffset, int> PrecipitationChance => _pc.Value;
    public ImmutableSortedDictionary<DateTimeOffset, int> CloudCoverPercent => _ccp.Value;
    public ImmutableSortedDictionary<DateTimeOffset, float> SoilMoisture => _sm.Value;

    public ImmutableSortedDictionary<DateOnly, (DateTimeOffset sunrise, DateTimeOffset sunset)> SunlightPerDay { get; init; } = null!;

    public dynamic Metadata { get; private init; }
    /// <summary>
    /// The time the data was produced
    /// </summary>
    /// <value></value>
    public DateTimeOffset Retrieved { get; init; }
}
