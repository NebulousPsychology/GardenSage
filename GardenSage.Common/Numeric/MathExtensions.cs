
namespace GardenSage.Common.Numeric;
public static class MathExtensions
{
    public static T P90<T>(this IEnumerable<T> values) where T : IComparable<T> => values.Percentile(90);

    public static T Percentile<T>(this IEnumerable<T> values, int percentile) where T : IComparable<T>
    {
        return values.Order().ElementAt((int)(values.Count() * percentile / 100.0));
    }
}
