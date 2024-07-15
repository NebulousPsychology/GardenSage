
using System.Device.Gpio.Drivers;

namespace GardenSage.Recorder;

public class Recorder(ILogger<Recorder> log) //: BackgroundService
{
    public static bool HasSensorHardware => TryGetHardwareInfo(out _, out _, null);

    public static bool TryGetHardwareInfo(
        out System.Device.ComponentInformation? info,
        out Exception? problem,
        System.Device.Gpio.GpioController? controller = null)
    {
#pragma warning disable SDGPIO0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        try
        {
            System.Device.Gpio.GpioController ctrl = controller ?? new();
            info = ctrl.QueryComponentInformation();
            problem = null;
            return true;
        }
        catch (GpiodException unsupported)
        {
            info = null;
            problem = unsupported;
            return false;
        }
#pragma warning restore SDGPIO0001
    }

    // protected override Task ExecuteAsync(CancellationToken stoppingToken)
    // {
    //     log.LogInformation("hello log world");
    //     return Task.CompletedTask;
    // }

    public TemperatureDifference GetTemperaturePair()
    {

        TemperatureDifference state = new(DateTimeOffset.Now,
            Indoor: Random.Shared.Next(50, 90),
            Outdoor: Random.Shared.Next(50, 90));
        log.LogInformation("SenseTemperature: {state}", state);
        return state;
    }
    public void AddFacilityState(FacilityState state)
    {
        log.LogInformation("AddFacilityState: {state}", state);
    }
}
