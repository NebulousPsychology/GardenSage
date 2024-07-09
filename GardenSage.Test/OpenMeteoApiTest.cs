namespace GardenSage.Test;

#pragma warning disable CS9113 // Parameter is unread.
public class OpenMeteoApiTest(Xunit.Abstractions.ITestOutputHelper log)
#pragma warning restore CS9113 // Parameter is unread.
{
#if USE_OPENMETEO_API
#if PERFORM_INTEGRATION_TESTS
    [Fact]
#else
    [Fact(Skip = "NO INTEGRATION TESTING")]
#endif
    public async void OpenMeteoIntegrationTest()
    {
        var client = new OpenMeteo.OpenMeteoClient();
        var uri = new Uri("https://api.open-meteo.com/v1/forecast?latitude=52.52&longitude=13.41&hourly=temperature_2m,windspeed_10m&daily=temperature_2m_max,temperature_2m_min&format=flatbuffers&timezone=auto");
        var results = await client.GetWeather(uri);

        var result = results[0];
        log.WriteLine($"Latitude: {result.Latitude}");
        log.WriteLine($"Longitude: {result.Longitude}");
        log.WriteLine($"Elevation: {result.Elevation}");
        log.WriteLine($"Timezone: {result.Timezone} (UTC offset {result.UtcOffsetSeconds} seconds)");

        var hourly = result.Hourly ?? throw new ArgumentNullException(nameof(result.Hourly));
        var temperature_2m = hourly.Variables(0)?.GetValuesArray() ?? throw new ArgumentNullException("temperature_2m");
        var windspeed_10m = hourly.Variables(1)?.GetValuesArray() ?? throw new ArgumentNullException("windspeed_10m");
        log.WriteLine("");
        log.WriteLine("hour | temperature_2m | windspeed_10m");
        for (var i = 0; i < (hourly.TimeEnd - hourly.Time) / hourly.Interval; i += 1)
        {
            // By adding `UtcOffsetSeconds` the print function below will print the local date
            var time = hourly.Time + i * hourly.Interval + result.UtcOffsetSeconds;
            log.WriteLine("{0} : {1} | {2}", DateTimeOffset.FromUnixTimeSeconds(time).ToString("yyyy-MM-dd HH:mm:ss"), temperature_2m[i], windspeed_10m[i]);
        }

        var daily = result.Daily ?? throw new ArgumentNullException(nameof(result.Daily));
        var temperature_2m_max = daily.Variables(0)?.GetValuesArray() ?? throw new ArgumentNullException("temperature_2m_max");
        var temperature_2m_min = daily.Variables(1)?.GetValuesArray() ?? throw new ArgumentNullException("temperature_2m_min");
        log.WriteLine("");
        log.WriteLine("day | temperature_2m_max | temperature_2m_min");
        for (var i = 0; i < (daily.TimeEnd - daily.Time) / daily.Interval; i += 1)
        {
            // By adding `UtcOffsetSeconds` the print function below will print the local date
            var time = daily.Time + i * daily.Interval + result.UtcOffsetSeconds;
            log.WriteLine("{0} | {1} | {2}", DateTimeOffset.FromUnixTimeSeconds(time).ToString("yyyy-MM-dd HH:mm:ss"), temperature_2m_max[i], temperature_2m_min[i]);
        }
        Assert.Fail("text");
    }
#endif
}
