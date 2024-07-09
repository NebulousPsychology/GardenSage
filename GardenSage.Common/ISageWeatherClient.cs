namespace GardenSage.Common;

/// <summary>
/// OpenMeteo data source (an adapter would be better)
/// </summary>
public interface ISageWeatherClient
{
    public Task<IForecastDataAdapter> GetWeatherAsync(ForecastParameters p);
}

