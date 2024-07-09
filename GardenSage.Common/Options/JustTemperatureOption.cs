// See https://aka.ms/new-console-template for more information
using GardenSage.Common;
// https://learn.microsoft.com/en-us/dotnet/core/extensions/workers#terminology

namespace GardenSage.Common.Options;
public class JustTemperatureOption
{
    public OMTemperature TemperatureUnits { get; set; }
}
