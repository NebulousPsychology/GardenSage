namespace GardenSage.Test.Mocks;

/// <summary>
/// 
/// winter (jan11): 26-40: d14, avg33
/// summer (aug15): 66-94: d28, avg80
/// -> avg(33,80)=56.5 is the year-carrier wave extents?  56.5 +/- 23.5
/// range in lows: 40
/// range in highs:54
/// s-w avghigh: 67
/// s-w avglow: 46
/// </summary>
public class SyntheticTemperature(
    double winterMin,
    double winterMax,
    double summerMin,
    double summerMax,
    TimeSpan? phaseOffset = null
)
{
    private static DateTime StartOfYear => new(DateTime.Today.Year, 1, 1);

    /// <summary>the northern winter solstice is ~december 22</summary>
    public static readonly TimeSpan PhaseNorthernHemisphere = TimeSpan.FromDays(-8);

    /// <summary>the southern winter solstice is ~june 21</summary>
    public static readonly TimeSpan PhaseSouthernHemisphere = TimeSpan.FromDays(173);

    /// <summary>
    /// some plausible values for the Northern Hemisphere
    /// </summary>
    /// <remarks>
    /// Some justification beyond bias:
    /// - 68% of landmass, 90% of global population
    /// - day temperatures rise then fall for both hemispheres, but only northern seasonal temperatures rise then fall as well: good symmetry
    /// </remarks>
    public static readonly SyntheticTemperature Default
        = new(summerMin: 67, summerMax: 95, winterMin: 5, winterMax: 41, phaseOffset: PhaseNorthernHemisphere);

    public const int HOURS_PER_YEAR = 8760;
    private const double TWOPI = 6.28318530717959;
    private const double TWOPI_OVER_24 = 0.261799387799149;
    private const double TWOPI_OVER_24x365 = 0.000717258596709998;

#pragma warning disable IDE1006 // Naming Styles
    ///<summary>constant governing the seasonal change in daily amplitude: summer varies more than winter</summary>
    private readonly double Ka = 0.5 * (summerMax - summerMin - winterMax + winterMin);

    /// <summary> the amplitute constant of the year-long seasonal carrier wave </summary>
    private readonly double Ay = 0.25 * (summerMax + summerMin - winterMax - winterMin);

    /// <summary> the avg temp across the year, intercept for the year-long seasonal carrier wave </summary>
    private readonly double Cy = 0.25 * (summerMax + summerMin + winterMax + winterMin);

    private readonly double _phaseOffset_hours = phaseOffset?.TotalHours ?? 0;
#pragma warning restore IDE1006 // Naming Styles

    /// <summary>
    /// 
    /// </summary>
    /// <value></value>
    public string Metadata
    {
        get
        {
            static dynamic MetaStats(double min, double max) => new
            {
                min,
                delta = max - min,
                average = (max - min) * 0.5,
                max,
            };
            dynamic Solstice(TimeSpan t) => new
            {
                Date = StartOfYear.Add(t).Date,
                Info = MetaStats(
                    Temperature(StartOfYear.Add(t).Date - StartOfYear),
                    Temperature(StartOfYear.Add(t).Date.AddHours(12) - StartOfYear))
            };
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                WinterSolsticePhase = phaseOffset ?? TimeSpan.Zero,
                summer = MetaStats(summerMin, summerMax),
                winter = MetaStats(winterMin, winterMax),
                Constants = new { Ka, Cy, Ay },
                WinterSolstice = Solstice(phaseOffset ?? TimeSpan.Zero),
                SummerSolstice = Solstice((phaseOffset ?? TimeSpan.Zero).Add(TimeSpan.FromDays(365 * 0.5)))
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
    }

    /// <summary>A_d(t)</summary>
    private double AmpModulation_Daily(double h) => Ka * (0.5 * Wave_Year(h, _phaseOffset_hours) + 1.5);

    /// <summary>Temp daily modulation wave</summary>
    private double TemperatureDailyModulationWave(double h) => AmpModulation_Daily(h) * Wave_Day(h);

    /// <summary>Temperature's yearly seasonal carrier wave</summary>
    private double TemperatureSeasonalCarrierWave(double h) => Ay * Wave_Year(h, _phaseOffset_hours) + Cy;

    /// <summary>
    /// A seasonal wave, modulated by daily highs and lows
    /// </summary>
    /// <param name="h">hour of year</param>
    /// <returns></returns>
    public double Temperature(double h) => TemperatureSeasonalCarrierWave(h) + TemperatureDailyModulationWave(h);

    /// <summary> 
    /// A seasonal wave, modulated by daily highs and lows, AND an additional perturbation: <code>3 * Math.Sin(h)</code>
    /// </summary>
    /// <param name="h"></param>
    /// <returns></returns>
    public double NoisyTemperature(double h) => 3 * Math.Sin(h) + Temperature(h);

    public double Temperature(TimeSpan t) => Temperature(t.TotalHours);
    public double NoisyTemperature(TimeSpan t) => NoisyTemperature(t.TotalHours);

    #region Statics
    /// <summary>
    /// a day-long fundamental waveform
    /// </summary>
    /// <param name="h"></param>
    /// <returns></returns>
    private static double Wave_Day(double h) => -Math.Cos(TWOPI_OVER_24 * h);
    /// <summary>
    /// a year-long fundamental waveform, with phase adjustment
    /// </summary>
    /// <param name="h"></param>
    /// <param name="phase"></param>
    /// <returns></returns>
    private static double Wave_Year(double h, double phase = 0) => -Math.Cos(TWOPI_OVER_24x365 * (h - phase));

    /// <summary>
    /// A fully controlled waveform function
    /// </summary>
    /// <param name="t"></param>
    /// <param name="wavelength"></param>
    /// <param name="amplitude"></param>
    /// <param name="intercept"></param>
    /// <param name="phaseOffset"></param>
    /// <returns></returns>
    public static double CarrierWave(double t, double wavelength,
        double amplitude = 1.0,
        double intercept = 0,
        double phaseOffset = 0) => amplitude * (-Math.Cos(TWOPI * (t - phaseOffset) / wavelength)) + intercept;

    [Obsolete("custom waves are no longer needed")]
    public static double AsPeakToPeakAmplitude(double amplitude) => amplitude / 2.0;
    [Obsolete("custom waves are no longer needed")]
    public static double AsMinIntercept(double min, double amplitude) => min + amplitude;
    [Obsolete("custom waves are no longer needed")]
    public static double AsPeakToPeakMinIntercept(double min, double amplitude) => min + (amplitude / 2.0);


    /// <summary>
    ///  a 24h carrier wave: 0 at midnight, peaking at <paramref name="trange"/> at noon
    ///  <code>0.5f * trange * (1 - MathF.Cos(h * MathF.PI / 12f))</code>
    /// </summary>
    /// <param name="h"></param>
    /// <param name="trange"></param>
    /// <returns></returns>
    public static double CarrierWave_Day(double h, double amplitude = 1f, double intercept = 0)
        => CarrierWave(h, 24, amplitude, intercept, 0);


    /// <summary>
    /// carrier wave modeling change: period of 1 year
    /// </summary>
    /// <param name="h">hour into the year</param>
    /// <param name="amplitude">peak-to-peak amplitude</param>
    /// <param name="intercept"></param>
    /// <param name="phaseOffset_hours"></param>
    /// <returns>value</returns>
    public static double CarrierWave_Year(double h, double amplitude = 1.0, double intercept = 0, double phaseOffset_hours = 0)
        => CarrierWave(h, 365 * 24, amplitude, intercept: intercept, phaseOffset: phaseOffset_hours);
    #endregion Statics
}
