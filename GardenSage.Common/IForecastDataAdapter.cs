using System.Collections.Immutable;
using System.Text;


namespace GardenSage.Common;

public interface IForecastDataAdapter
{
    public DateTimeOffset Retrieved { get; init; }
    public dynamic Metadata { get; }
    public ImmutableSortedDictionary<DateTimeOffset, int> CloudCoverPercent { get; }
    public ImmutableSortedDictionary<DateTimeOffset, int> PrecipitationChance { get; }
    public ImmutableSortedDictionary<DateTimeOffset, float> Temperature { get; }
    public ImmutableSortedDictionary<DateTimeOffset, float> SoilMoisture { get; }
    public ImmutableSortedDictionary<DateOnly, (DateTimeOffset sunrise, DateTimeOffset sunset)> SunlightPerDay { get; }

    public string Summary
    {
        get
        {
            StringBuilder s = new();
            foreach (var day in Temperature.GroupBy(t => t.Key.Date))
            {
                s.AppendLine($"{day.Key.ToShortDateString()}: low:{day.Min(o => o.Value)} | high: {day.Max(o => o.Value)}");
            }
            return s.ToString();
        }
    }
}
