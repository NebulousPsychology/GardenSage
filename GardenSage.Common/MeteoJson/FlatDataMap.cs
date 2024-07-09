using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GardenSage.Common.MeteoJson;
public class FlatDataMap
{
    [System.Text.Json.Serialization.JsonExtensionData()]
    public Dictionary<string, JsonElement> RawDataMembers { get; init; } = [];

    // private Dictionary<string, Type> _typemap = new() { ["time"] = typeof(DateOnly) };

    public IEnumerable<string> PropertyNames => RawDataMembers.Keys;

    /// <summary>
    /// Get the number of elements in the named array
    /// </summary>
    /// <param name="arrayName"></param>
    /// <returns></returns>
    public int? CountOf(string arrayName)
    {
        return RawDataMembers.TryGetValue(arrayName, out JsonElement value)
            ? value.GetArrayLength() : null;
    }

    public IEnumerable<TValue> ResolveEnumerable<TValue>(string key)
    {
        return RawDataMembers.TryGetValue(key, out JsonElement value)
            ? value.Deserialize<IEnumerable<TValue>>()
                ?? throw new InvalidCastException($"trying to cast element '{key}' to IEnumerable<{typeof(TValue)}>")
            : throw new KeyNotFoundException($"{key} not found");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="InvalidCastException"></exception>
    /// <exception cref="KeyNotFoundException"></exception>
    public TValue[] ResolveArray<TValue>(string key)
    {
        return RawDataMembers.TryGetValue(key, out JsonElement value)
            ? value.Deserialize<TValue[]>() ?? throw new InvalidCastException($"trying to cast{key} to {typeof(TValue)}")
            : throw new KeyNotFoundException($"{key} not found");
    }

    public OutValue[] ResolveArrayAs<InValue, OutValue>(string key, Func<InValue, OutValue> conversion)
    {
        return RawDataMembers.TryGetValue(key, out JsonElement value)
            ? value.Deserialize<InValue[]>()?.Select(inval => conversion(inval)).ToArray()
                ?? throw new InvalidCastException($"trying to cast{key} to {typeof(InValue)}")
            : throw new KeyNotFoundException($"{key} not found");
    }


    public ImmutableSortedDictionary<TTime, TTransformed> Resolve<TTime, TValue, TTransformed>(string valuekey, Func<TValue, TTransformed> convert, string timekey = "time")
        where TTime : struct, IComparable, IFormattable, IConvertible
    {
        TTime[] times = ResolveArray<TTime>(timekey);
        TTransformed[] values = ResolveArray<TValue>(valuekey).Select(convert).ToArray();
        if (times.Length != values.Length)
            throw new RankException($"{times.Length} timekeys mismatches {values.Length} values");

        return Enumerable.Range(0, times.Length)
            .ToImmutableSortedDictionary(keySelector: i => times[i], elementSelector: i => values[i]);
    }

    public ImmutableSortedDictionary<TTime, TValue> Resolve<TTime, TValue>(string valuekey, string timekey = "time")
        where TTime : struct, IComparable, IFormattable, IConvertible
    {
        TTime[] times = ResolveArray<TTime>(timekey);
        TValue[] values = ResolveArray<TValue>(valuekey);
        if (times.Length != values.Length)
            throw new RankException($"{times.Length} timekeys mismatches {values.Length} values");

        return Enumerable.Range(0, times.Length).ToImmutableSortedDictionary(keySelector: i => times[i], elementSelector: i => values[i]);
    }
}
