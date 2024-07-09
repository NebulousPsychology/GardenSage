namespace GardenSage.Common;

public record FacilityState
{
    public FacilityState(KeyValuePair<DateTimeOffset, float> tempAtTime, bool ac, bool furnace, bool tryVent)
    {
        if (ac && furnace) throw new ArgumentException("ac and furnace will never be used at the same time");
        Time = tempAtTime.Key;
        Temperature = tempAtTime.Value;
        OnAC = ac;
        OnFurnace = furnace;
        VentilationOpen = !furnace && !ac && tryVent;
    }
    public DateTimeOffset Time;
    /// <summary>
    /// AC should be set On, with normal goal
    /// </summary>
    public bool OnAC;
    /// <summary>
    /// AC should be set On, with normal goal
    /// </summary>
    public bool OnFurnace;
    public bool VentilationOpen;
    public double Temperature { get; set; }
    public bool HasChangedStateFrom(FacilityState prev)
    {
        return prev.VentilationOpen != VentilationOpen
            || prev.OnAC != OnAC
            || prev.OnFurnace != OnFurnace;
    }

    public string ToShortString()
        => $"{Math.Round(Temperature, 2):N1} {(OnAC ? 'A' : '-')}{(OnFurnace ? "F" : '-')}{(VentilationOpen ? "V" : '-')} @ {Time}";

}
