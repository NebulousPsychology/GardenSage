using GardenSage.Common.MeteoJson;
using GardenSage.Common.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GardenSage.Common;

/// <summary>
/// Produce adapted data, usaually from a client
/// </summary>
[Obsolete("use ForecastRetrievalService directly")]
public interface IForecastRetrievalService
{
    public IForecastDataAdapter Data { get; }
}


/// <summary>
/// produce adapted data from an ISageWeatherClient
/// </summary>
/// <typeparam name="SageOptions"></typeparam>
public class ForecastRetrievalService : IForecastRetrievalService
{
    private readonly Lazy<Task<IForecastDataAdapter>> _fetchedData;
    private ISageWeatherClient Client { get; set; }
    private ILogger<ForecastRetrievalService> Log { get; set; }

    public ForecastRetrievalService(
        ILoggerFactory logger,
        IOptions<SageOptions> options,
        IOptions<LocalJsonClient.LocalClientOptions> clientopts
    )
    {
        this.Log = logger.CreateLogger<ForecastRetrievalService>();
        this.Client = clientopts.Value.LocalClientPath is not null ?
            new LocalJsonClient(logger.CreateLogger<LocalJsonClient>(), clientopts) :
            new MeteoJsonClient();
        Log.LogDebug("Preparing a {type}", Client.GetType());
        _fetchedData = new(valueFactory:
        () => Task.Run(async () => await Client.GetWeatherAsync(ForecastParameters.FromOptions(options.Value))));
    }

    public IForecastDataAdapter Data => _fetchedData.Value.Result;
}
