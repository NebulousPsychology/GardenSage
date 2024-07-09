using System.Globalization;

using GardenSage.Common.Options;

namespace GardenSage.Common;

public struct ForecastParameters
{
    public double latitude;
    public double longitude;
    public int lookBehind = 2;
    public int lookAhead = 7;
    public string timezone = "auto";
    public OMTemperature tempFormat = OMTemperature.celsius;
    public OMDataFormat format = OMDataFormat.json;

    public ForecastParameters() { }
    public static ForecastParameters FromOptions(SageOptions option)
    {
        return new ForecastParameters
        {
            latitude = option.Latitude,
            longitude = option.Longitude,
            lookBehind = option.lookBehind,
            lookAhead = option.lookAhead,
            timezone = "auto",
            tempFormat = option.TemperatureUnits,
            format = OMDataFormat.json,
        };
    }
}


public enum OMDataFormat
{
    json,
    flatbuffers,
}
