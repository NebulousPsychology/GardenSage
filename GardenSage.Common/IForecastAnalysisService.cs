namespace GardenSage.Common;

public interface IForecastAnalysisService
{
    public dynamic Metadata { get; }

    public DateTimeOffset? BestTimeToBlower(TimeSpan taskDuration, float? threshold);

    public DateTimeOffset? OpenWindowsAt(DateOnly date);

    /// <summary>
    /// Find times to open up
    /// // when the temp rises above threshold
    /// </summary>
    /// <param name="date"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public IEnumerable<DateTimeOffset> OpenWindowsAt();

    /// <summary>
    /// Find times to  batten down when the temp rises above threshold
    /// </summary>
    /// <param name="date"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public IEnumerable<DateTimeOffset> CloseWindowsAt();

    public DateTimeOffset? CloseWindowsAt(DateOnly date);

    public IEnumerable<string> WriteForecast();
}
