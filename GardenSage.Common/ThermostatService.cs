
using System.Text.Json;

using GardenSage.Common.Numeric;
using GardenSage.Common.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace GardenSage.Common;
public class ThermostatService
{
    public ThermostatService(
        ILogger<ThermostatService> logger,
        IOptions<ThermostatOptions> options,
        IOptions<JustTemperatureOption> temps,
        ForecastRetrievalService forecast)
    {
        Log = logger;
        _options = options;
        _forecast = forecast;
        _facilityStates = new(valueFactory: () => GetFacilityManagmentStates());
        Log.LogInformation("Thermostat using settings: {opts}", JsonSerializer.Serialize(options.Value));
    }

    private ILogger<ThermostatService> Log { get; set; }

    private readonly IOptions<ThermostatOptions> _options;
    private ThermostatOptions Cfg => _options.Value;
    private readonly ForecastRetrievalService _forecast;

    public delegate TOutput RunningAggregateFunc<TInput, TOutput>(TInput previous, TInput current);
    /// <summary>
    /// filters the dataset based on the previous value
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="data"></param>
    /// <param name="condition"> (current, previous) => bool </param>
    /// <param name="includeFirst">prepend the first element (which would not otherwise have a previous state to compare against</param
    /// <returns></returns>
    public static IEnumerable<TValue>
        HindsightedWhere<TValue>(IEnumerable<TValue> data, RunningAggregateFunc<TValue, bool> condition, bool includeFirst = true)
    {
        var filtered = data
            .Zip(data.Skip(1),
                (prevPair, cur) => new { Current = cur, Previous = prevPair })
            .Where(pair => condition(previous: pair.Previous, current: pair.Current))
            .Select(pair => pair.Current)
            ;
        return includeFirst ? filtered.Prepend(data.First()) : filtered;
    }


    readonly private Lazy<IEnumerable<FacilityState>> _facilityStates;
    public IEnumerable<FacilityState> FacilityStates => _facilityStates.Value;

    /// <summary>
    /// Find times to  batten down when the temp rises above threshold
    /// </summary>
    /// <param name="date"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public IEnumerable<FacilityState> CloseWindowsAt()
    {
        var closesAt = HindsightedWhere(FacilityStates, (prev, cur) =>
            prev.VentilationOpen != cur.VentilationOpen && false == cur.VentilationOpen);
        return closesAt;
    }

    public IEnumerable<FacilityState> OpenWindowsAt()
    {
        var opensAt = HindsightedWhere(FacilityStates, (prev, cur) =>
            prev.VentilationOpen != cur.VentilationOpen && true == cur.VentilationOpen);
        return opensAt;
    }

    /// <summary>
    /// get all the stateswhere a state change occurs
    /// </summary>
    /// <returns></returns>
    public IEnumerable<FacilityState> ChangedStates() => HindsightedWhere(
        data: FacilityStates,
        condition: (prev, cur) => cur.HasChangedStateFrom(prev));


    /// <summary>
    /// the logic around open/close of windows is influenced by how much of the day will be above the threshold
    /// percent of day Too Hot/Too Cold/Just Right,  hysteresis in thermostats
    /// </summary>
    /// <param name="day"></param>
    /// <returns></returns>
    protected static float GetThermalRatio(IEnumerable<KeyValuePair<DateTimeOffset, float>> datapointsInDay, Func<float, bool> condition)
    {
        // if most of the day is too hot, batten down when we rise above T_cold, open when we fall below T_hot
        // if most of the day is too cold, 
        // IEnumerable<KeyValuePair<DateTimeOffset, float>> datapointsInDay = Data.Temperature.Where(t => DateOnly.FromDateTime(t.Key.Date) == day);
        if (!datapointsInDay.Any())
            throw new InvalidDataException($"no datapoints");

        float datapointsAboveThreshold = datapointsInDay.Count(t => condition(t.Value));
        return datapointsAboveThreshold / datapointsInDay.Count();
    }

    /// <summary>
    /// Compute Facility state for every hour of the forecast
    /// </summary>
    /// <returns></returns>
    protected IEnumerable<FacilityState> GetFacilityManagmentStates()
    {
        using (Log.BeginScope(nameof(GetFacilityManagmentStates)))
        {
            var days = _forecast.Data.Temperature.GroupBy(t => t.Key.Date);
            //.Where(t => DateOnly.FromDateTime(t.Key.Date) == day);
            // if (!datapointsInDay.Any()) throw new InvalidDataException($"no datapoints for {day}");
            foreach (IGrouping<DateTime, KeyValuePair<DateTimeOffset, float>> day in days)
            {
                // !! oddity in states is because grouping by day creates a hard boundary when finding thermal ratio
                // TODO: try using increments above/below goldilocks?
                // ? how successful is AC/Furnace at reaching goal temp? ()
                // thermostat fusion
                float dayp90 = day.Select(g => g.Value).P90();
                Log.LogTrace("Evaluating facility states for {date}, p90={p}", day.Key.ToShortDateString(), dayp90);
                float percentAboveHot = GetThermalRatio(day, Cfg.IsAboveHot);
                float percentBelowCold = GetThermalRatio(day, Cfg.IsBelowCold);

                bool coldEnoughForFurnace = Cfg.ApplianceRatio < percentBelowCold;
                bool hotEnoughForAC = Cfg.ApplianceRatio < percentAboveHot;
                using (Log.BeginScope(day.Key.ToShortDateString()))
                {
                    Log.LogDebug("Saw {n} facility points for {date}", day.Count(), day.Key.ToShortDateString());
                    foreach (var tempAtTime in day)
                    {
                        // if using AC today, and temp is goldilocks or hot, turn on ac
                        bool ac = hotEnoughForAC && !Cfg.IsBelowCold(tempAtTime.Value)
                            || Cfg.MagnituedVersusGoldilocks(tempAtTime.Value) > 2.0;

                        // if using furnace today, and temp is goldilocks or cold, turn on furnace
                        bool furnace = coldEnoughForFurnace && !Cfg.IsAboveHot(tempAtTime.Value)
                            || Cfg.MagnituedVersusGoldilocks(tempAtTime.Value) < -2.0;

                        // without considering thermal appliances, when to open the window?
                        int normalWindowOpen = tempAtTime.Value switch
                        {
                            // explanations below
                            float t when Cfg.IsAboveHot(t) && percentAboveHot > Cfg.VentRatio => -1,
                            float t when !Cfg.IsAboveHot(t) && percentAboveHot > Cfg.VentRatio => 1,
                            float t when Cfg.IsBelowCold(t) && percentBelowCold > Cfg.VentRatio => -2,
                            float t when !Cfg.IsBelowCold(t) && percentBelowCold > Cfg.VentRatio => 2,
                            _ => 0
                        };
                        if (Log.IsEnabled(LogLevel.Debug))
                            Log.LogDebug("{time} @{temp}: {msg} (ac:{ac},furnace:{furnace})", tempAtTime.Key, tempAtTime.Value, normalWindowOpen switch
                            {
                                -1 => "hot, on a hot day: windows closed",
                                +1 => "goldilocks or cold, on a hot day: windows open if appliances off",
                                -2 => "cold, on a cold day: windows closed",
                                +2 => "goldilocks or hot, on a cold day: windows open if appliances off",
                                _ => throw new Exception(
                                    string.Format("unrecognized state {0} = f({1}deg,h%{2},c%{3})",
                                    normalWindowOpen, tempAtTime.Value, percentAboveHot, percentBelowCold)
                                    ),
                            }, ac ? "on" : "off", furnace ? "on" : "off");
                        FacilityState state = new(tempAtTime, ac: ac, furnace: furnace, tryVent: normalWindowOpen > 0);
                        Log.LogDebug("Produced {state}", state);
                        yield return state;
                    } // /foreach hour
                }
            }// /foreach day
        }
    }
}
