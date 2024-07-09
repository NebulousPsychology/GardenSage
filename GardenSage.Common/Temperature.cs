namespace GardenSage.Common;

public readonly struct Temperature(double value, OMTemperature kind)
    : IComparable<Temperature>, IComparable<double>
{
    public double Value { get; init; } = value;
    public OMTemperature Kind { get; init; } = kind;

    public static double ToFarenheit(double c) => (1.8 * c) + 32;
    public static double ToCelsius(double f) => (f - 32) / 1.8;
    public Temperature ToFarenheit() => OMTemperature.fahrenheit == Kind ? this
        : new(ToFarenheit(Value), OMTemperature.fahrenheit);
    public Temperature ToCelsius() => OMTemperature.celsius == Kind ? this
        : new(ToCelsius(Value), OMTemperature.celsius);

    public int CompareTo(Temperature other) => ToCelsius().Value.CompareTo(other.ToCelsius().Value);

    public int CompareTo(double other) => Value.CompareTo(other);
}

public enum OMTemperature
{
    celsius,
    fahrenheit,
    C = celsius,
    F = fahrenheit
}
