
namespace GardenSage.Common;

public static class ForecastUriFactory
{
    #region uri factory
    public static string EncodeTimezone(string timezone)
    {
        string encodedtz = timezone.Contains("%2f", StringComparison.InvariantCultureIgnoreCase)
            ? timezone : System.Web.HttpUtility.UrlEncode(timezone);
        return encodedtz;
    }


    public static Uri CreateUri(ForecastParameters p) => CreateUriBase(p, "https://api.open-meteo.com/v1/forecast");
    public static Uri CreateDocsUri(ForecastParameters p) => CreateUriBase(p, "https://open-meteo.com/en/docs");

    static readonly IEnumerable<string> Dailyprops = [
            "weather_code",
            "temperature_2m_max",
            "sunrise", "sunset",
            "precipitation_hours",//
            "precipitation_probability_mean",
            "precipitation_sum"
            ];
    static readonly IEnumerable<string> Hourlyprops = [
            "temperature_2m",
            "precipitation_probability",
            "precipitation",
            "cloud_cover",
            // # "rain,showers,snow"
            "soil_moisture_0_to_1cm",
        ];

    private static Uri CreateUriBase(ForecastParameters p, string baseuri)
    {
        Dictionary<string, string> queryitems = new()
        {
            ["latitude"] = p.latitude.ToString(),
            ["longitude"] = p.longitude.ToString(),
            ["hourly"] = string.Join(",", Hourlyprops), // or add to query with a Concat
            ["daily"] = string.Join(",", Dailyprops), // or add to query with a Concat
            ["temperature_unit"] = p.tempFormat.ToString(),
            // ["wind_speed_unit"]="mph",
            ["timezone"] = EncodeTimezone(p.timezone),
            ["past_days"] = p.lookBehind.ToString(),
            ["forecast_days"] = p.lookAhead.ToString(),
            ["format"] = p.format.ToString(),
        };
        var builder = new UriBuilder(baseuri)
        {
            Query = string.Join("&", queryitems.Select(kvp => $"{kvp.Key}={kvp.Value}"))
        };
        return builder.Uri;
    }

    public static Uri CreateUri(float latitude, float longitude,
        int lookBehind = 0, int lookAhead = 7,
        string timezone = "auto",
        OMTemperature tempFormat = OMTemperature.fahrenheit,
        OMDataFormat format = OMDataFormat.json) => CreateUri(new()
        {
            format = format,
            latitude = latitude,
            longitude = longitude,
            lookAhead = lookAhead,
            lookBehind = lookBehind,
            tempFormat = tempFormat,
            timezone = timezone
        });
    #endregion
}
