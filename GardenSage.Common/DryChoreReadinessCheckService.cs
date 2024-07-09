
using Microsoft.Extensions.Logging;
namespace GardenSage.Common;

public class DryChoreReadinessCheckService(
    ILogger<DryChoreReadinessCheckService> logger,
    ForecastRetrievalService forecast
    )
{
    public bool TryGetBestDayForDryChore(float? threshold,
        out DateTimeOffset? bestTime, out float? confidence)
    {
        using (logger.BeginScope(nameof(TryGetBestDayForDryChore)))
        {
            // get moistrue,precip,time tuples
            var info = forecast.Data.SoilMoisture.Join(forecast.Data.PrecipitationChance,
                outerKeySelector: o => o.Key, innerKeySelector: i => i.Key,
                resultSelector: (okvp, ikvp) => (Precip: ikvp.Value, Moisture: okvp.Value, Time: okvp.Key));

            (int precip, float moisture, DateTimeOffset time) = info
                .Where(tup => tup.Precip == 0 && tup.Moisture < (threshold ?? float.MaxValue))
                .MinBy(tup => tup.Moisture);

            confidence = moisture;
            bestTime = time;
            return true;
        }
    }
}
