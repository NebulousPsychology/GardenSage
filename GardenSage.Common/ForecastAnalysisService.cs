
namespace GardenSage.Common;

using System.Text.Json;
using System.Threading;

using GardenSage.Common.Numeric;
using GardenSage.Common.Options;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// what questions will GardenSage need to answer from forecast data?
/// A: when to close/open windows
/// B: when blower will be most effective
/// 
/// option: daylight/anytime
/// 
/// using:
/// - soil moisture (B: minimize)
/// - precipitation chance (B: will finish task just before rains)
/// - temperature (A: gt Threshold)
/// </summary>
/// <remarks>
/// # https://open-meteo.com/
/// https://api.open-meteo.com/v1/forecast?latitude=47.620529&longitude=-122.349297&hourly=temperature_2m,precipitation_probability,precipitation,cloud_cover,soil_moisture_0_to_1cm&daily=weather_code,temperature_2m_max,sunrise,sunset,precipitation_hours,precipitation_probability_mean,precipitation_sum&temperature_unit=fahrenheit&wind_speed_unit=mph&timezone=America%2FLos_Angeles&past_days=2
/// </remarks>
public class ForecastAnalysisService(
    IHostApplicationLifetime lifetime,
    ILogger<ForecastAnalysisService> logger,
    IOptions<SageOptions> options,
    ForecastRetrievalService forecastDataFetcher,
    DryChoreReadinessCheckService dryChecker,
    ThermostatService thermostat
    )
    : BackgroundService, IForecastAnalysisService
{
    IForecastDataAdapter Data => _forecastDataFetcher.Data;

    public dynamic Metadata => Data.Metadata;

    #region Ventilation
    private readonly ForecastRetrievalService _forecastDataFetcher = forecastDataFetcher
        ?? throw new ArgumentNullException(nameof(forecastDataFetcher));


    /// <summary>
    /// Find times to  batten down when the temp rises above threshold
    /// </summary>
    /// <param name="date"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public IEnumerable<DateTimeOffset> CloseWindowsAt()
        => thermostat.CloseWindowsAt().Select(f => f.Time);

    /// <summary>
    /// Find times to  open up when the temp rises above threshold
    /// </summary>
    /// <param name="date"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public IEnumerable<DateTimeOffset> OpenWindowsAt()
        => thermostat.OpenWindowsAt().Select(f => f.Time);

    public DateTimeOffset? CloseWindowsAt(DateOnly date)
        => CloseWindowsAt().SingleOrDefault(d => d.Date.Equals(date));


    /// <summary>
    /// Find times to  open up when the temp rises above threshold
    /// </summary>
    /// <param name="date"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public DateTimeOffset? OpenWindowsAt(DateOnly date)
        => OpenWindowsAt().SingleOrDefault(d => d.Date.Equals(date));

    #endregion

    #region GroundMoisture
    public DateTimeOffset? BestTimeToBlower(TimeSpan taskDuration, float? threshold = null)
    {
        dryChecker.TryGetBestDayForDryChore(threshold, out DateTimeOffset? t, out float? confidence);
        return t;
    }
    #endregion

    public IEnumerable<string> WriteForecast()
    {
        yield return $"------------------";
        yield return $"Retrieved: {Data.Retrieved.Date}";
        yield return $"Report metadata {this.Metadata.ToString()}";
        yield return $" close at [{string.Join(",", this.CloseWindowsAt())}]";
        yield return $" open at {OpenWindowsAt(DateOnly.FromDateTime(Data.Retrieved.Date))?.ToString() ?? "no open"}";
        yield return $" blower at {BestTimeToBlower(TimeSpan.FromMinutes(2))?.ToString() ?? "No available tiem"}";
        yield return $"------------------";
        var changes = thermostat.ChangedStates();
        foreach (var day in thermostat.FacilityStates.GroupBy(f => f.Time.Date))
        {
            yield return "";
            var temps = day.Select(d => d.Temperature);
            yield return $"{day.Key.ToShortDateString()} avg={temps.Average():N2} p90={temps.P90():N2} p50={temps.Percentile(50):N2}"; 
            foreach (FacilityState f in day)
            {
                // var maxmin = Data.Temperature[f.Time] == day.Max(h => h.Temperature) ? ' ':' ';
                var maxmin = Data.Temperature[f.Time] switch
                {
                    float t when t == day.Max(h => h.Temperature) => '+',
                    float t when t == day.Min(h => h.Temperature) => '-',
                    _ => ' ',
                };
                char ischange = changes.Contains(f) ? '!' : ' ';
                yield return $"{maxmin}{ischange}  {f.ToShortString()}";
            }
            // yield return $"  State: {JsonSerializer.Serialize(f, new JsonSerializerOptions { MaxDepth=5 })}";
        }
        yield return $"------------------";

        yield break;
    }


    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (logger.BeginScope(nameof(ExecuteAsync)))
        {
            logger.LogDebug("fetching weather starting up");
            logger.LogInformation("Using Options: {opt}", JsonSerializer.Serialize(options.Value));
            await Task.Run(() =>
            {
                logger.LogDebug("fetching weather running");
                logger.LogInformation("{msg}", string.Join(Environment.NewLine, WriteForecast()));
                logger.LogInformation("{msg}", Data.Summary);
                foreach (var daystates in thermostat.ChangedStates().GroupBy(d => d.Time.Date))
                {
                    logger.LogInformation("Timeline {d}: {msg}", daystates.Key.ToShortDateString(),
                        string.Join("", daystates.Select(s => $"{Environment.NewLine}{s}")));
                }
                logger.LogInformation("days :{d}", (Data.Temperature.Keys.Last() - Data.Temperature.Keys.First()).TotalDays);
                lifetime.StopApplication();
            }, stoppingToken);
            logger.LogDebug("fetching weather wrapping up");
        }
    }
}
