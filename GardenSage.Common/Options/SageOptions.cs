
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace GardenSage.Common.Options;
public class SageOptions : JustTemperatureOption
{
    public const double DEFAULT_LATITUDE = 47.621212; // 47.620529;
    public const double DEFAULT_LONGITUDE = -122.33495; // -122.349297;

    [Required]
    [Range(-90, 90)]
    public double Latitude { get; init; } = DEFAULT_LATITUDE;

    [Required]
    [Range(-180, 180)]
    public double Longitude { get; init; } = DEFAULT_LONGITUDE;

    [Range(0, 7)]
    public int lookBehind { get; init; } = 2;

    [Required]
    [Range(2, 7)]
    public int lookAhead { get; init; } = 7;

    public string timezone { get; init; } = "auto";

    // [EnumDataType(typeof(OMTemperature))]
    // public OMTemperature TemperatureUnits { get; init; } = OMTemperature.celsius;

    [EnumDataType(typeof(OMDataFormat))]
    public OMDataFormat format { get; init; } = OMDataFormat.json;
}
