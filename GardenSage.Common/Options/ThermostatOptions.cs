
using System.ComponentModel.DataAnnotations;

using openmeteo_sdk;

namespace GardenSage.Common.Options;

public sealed class ThermostatOptions
{
    public const string ConfigurationSectionName = "Thermostat";

    [Required]
    [AllowedValues("fahrenheit", "celsius")]
    public OMTemperature Units { get; set; } = OMTemperature.fahrenheit;

    [Required]
    public double GoalTemperature { get; set; }

    [Required]
    public double TCold { get; set; } = 66;

    [Required]
    public double THot { get; set; } = 76;

    /// <summary>
    /// percent of day that must break goldibefore using ventilation
    /// </summary>
    [Range(0.0, 1.0)]
    public double VentRatio { get; set; } = 0.5;

    /// <summary>
    /// percent of day that must be appliance-worthy before using hvac
    /// </summary>
    /// <value></value>
    [Range(0.0, 1.0)]
    public double ApplianceRatio { get; set; } = 0.6;

    public double Goldilocks => (TCold + THot) * 0.5f;
    public double GoldilocksIncrement => Math.Abs(THot - TCold) switch
    {
        0 => 5,
        double t => t * 0.5,
    };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="temp"></param>
    /// <returns>negative scalar when t below goldilocks</returns>
    public double MagnituedVersusGoldilocks(double temp)
        => Math.Abs(temp - Goldilocks)
            / GoldilocksIncrement
            * (temp > Goldilocks ? +1 : -1);

    public bool IsAboveHot(float t) => THot < t;
    public bool IsBelowCold(float t) => t < TCold;
    public bool IsAboveCold(float t) => !IsBelowCold(t);
    public bool IsBelowHot(float t) => !IsAboveHot(t);
}
