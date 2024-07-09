using GardenSage.Common;
using GardenSage.Common.Options;
namespace GardenSage.Test;
public class ForecastUriFactoryTests(Xunit.Abstractions.ITestOutputHelper log)
{
    /// <summary>
    /// The api url used to fetch test data samples
    /// </summary>
    const string DataSampleUrl = "https://api.open-meteo.com/v1/forecast?latitude=47.621212&longitude=-122.33495&hourly=temperature_2m,precipitation_probability,precipitation,cloud_cover,soil_moisture_0_to_1cm&daily=weather_code,temperature_2m_max,sunrise,sunset,precipitation_hours,precipitation_probability_mean,precipitation_sum&temperature_unit=fahrenheit&timezone=America%2fLos_Angeles&past_days=2&forecast_days=7&format=json";


    [InlineData(SageOptions.DEFAULT_LATITUDE, SageOptions.DEFAULT_LONGITUDE, "America/New_York", "America%2FNew_York")]
    [InlineData(SageOptions.DEFAULT_LATITUDE, SageOptions.DEFAULT_LONGITUDE, "America%2FLos_Angeles", "America%2FLos_Angeles")]
    [InlineData(SageOptions.DEFAULT_LATITUDE, SageOptions.DEFAULT_LONGITUDE, "GMT", "GMT")]
    [InlineData(SageOptions.DEFAULT_LATITUDE, SageOptions.DEFAULT_LONGITUDE, "auto", "auto")]
    [Theory]
    public void TimezoneEscaping(float latitude, float longitude, string tz, string expectTz)
    {
        string encodedtz = ForecastUriFactory.EncodeTimezone(tz);
        _ = latitude.ToString();
        _ = longitude.ToString();
        Assert.Equal(expectTz, actual: encodedtz, ignoreCase: true);
    }

    [InlineData(SageOptions.DEFAULT_LATITUDE, SageOptions.DEFAULT_LONGITUDE, "America/New_York", "America%2FNew_York")]
    [InlineData(SageOptions.DEFAULT_LATITUDE, SageOptions.DEFAULT_LONGITUDE, "America%2FLos_Angeles", "America%2FLos_Angeles")]
    [InlineData(SageOptions.DEFAULT_LATITUDE, SageOptions.DEFAULT_LONGITUDE, "GMT", "GMT")]
    [InlineData(SageOptions.DEFAULT_LATITUDE, SageOptions.DEFAULT_LONGITUDE, "auto", "auto")]
    [Theory]
    public void UriProduction(float latitude, float longitude, string tz, string expectTz)
    {
        log.WriteLine("Hello");
        int lookBehind = 2, lookAhead = 7;
        string encodedtz = ForecastUriFactory.EncodeTimezone(tz);
        Assert.Equal(expectTz, actual: encodedtz, ignoreCase: true);

        Uri u = ForecastUriFactory.CreateUri(latitude, longitude, lookBehind, lookAhead, timezone: encodedtz);
        string actual = u.ToString();
        log.WriteLine("uri: <{0}>", u.ToString());
        Assert.Contains(expectTz, actual, StringComparison.InvariantCultureIgnoreCase);
        // OpenMeteo.OpenMeteoClient client = new();
        // openmeteo_sdk.WeatherApiResponse[] w = await client.GetWeather(u.Uri);

        // w[0].Hourly!.Value.Variables()

        // Assert.Fail("log output");
    }

    [Fact]
    public void ProducesCanonicalUri()
    {
        // Given
        string expected = DataSampleUrl;
        ForecastParameters param = new()
        {
            latitude = SageOptions.DEFAULT_LATITUDE,
            longitude = SageOptions.DEFAULT_LONGITUDE,
            tempFormat = OMTemperature.fahrenheit,
            timezone = ForecastUriFactory.EncodeTimezone("America/Los_Angeles"),
            // timezone="America/New_York",
        };
        // When
        Assert.Equal(expected, ForecastUriFactory.CreateUri(param).ToString());

        // Then
    }
}
