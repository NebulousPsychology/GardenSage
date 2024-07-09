using System.Collections.Immutable;

#if USE_OPENMETEO_LIB
using OpenMeteo;
namespace GardenSage.Common;
/// <summary>
/// ataptor-returning client subclass
/// </summary>
public class MeteoClient : OpenMeteo.OpenMeteoClient, IOMClient
{
    public async Task<IForecastDataAdapter> GetWeatherAsync(ForecastParameters p)
    {
        openmeteo_sdk.WeatherApiResponse[] data = await this.GetWeather(ForecastService.CreateUri(p));
        return new OpenMetroAdapter(data);
    }
    class OpenMetroAdapter
        : IForecastDataAdapter
    {
        public OpenMetroAdapter(openmeteo_sdk.WeatherApiResponse[] responses){
            var tz = TimeSpan.FromSeconds(responses[0].UtcOffsetSeconds);
            long? t = responses[0].Hourly?.Time;
            var u = responses[0].Hourly?.VariablesLength;
            var v = responses[0].Hourly?.Variables(0)?.Unit;
            var w = responses[0].Hourly?.Variables(0)?.GetValuesArray(); ;
            var x = responses[0].Hourly?.Variables(0)?.Variable.ToString();
        }
        public ImmutableSortedDictionary<DateTimeOffset, int> CloudCoverPercent => throw new NotImplementedException();

        public ImmutableSortedDictionary<DateTimeOffset, int> PrecipitationChance => throw new NotImplementedException();

        public ImmutableSortedDictionary<DateTimeOffset, float> Temperature => throw new NotImplementedException();

        public ImmutableSortedDictionary<DateTimeOffset, float> SoilMoisture => throw new NotImplementedException();

        public ImmutableSortedDictionary<DateOnly, (DateTimeOffset sunrise, DateTimeOffset sunset)> SunlightPerDay => throw new NotImplementedException();
    }
}
#endif
