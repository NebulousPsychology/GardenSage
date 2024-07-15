using System.Device.Gpio.Drivers;

using NLog;
using NLog.Extensions.Logging;
using NLog.Web;

using GardenSage.Recorder;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// https://github.com/NLog/NLog.Extensions.Logging/wiki/NLog-configuration-with-appsettings.json
NLog.LogManager.Configuration = new NLogLoggingConfiguration(builder.Configuration.GetSection("NLog"));
builder.Logging.AddNLogWeb(LogManager.Configuration);
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

logger.Info("service building");
logger.Info(builder.Configuration.GetDebugView());

builder.Services.AddScoped<Recorder>();
var app = builder.Build();

var log = app.Services.GetRequiredService<ILogger<Program>>();
log.LogInformation("Starting ups");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/heartbeat", getheartbeat).WithName("GetHeartbeat").WithOpenApi();
app.MapGet("/weatherforecast", getforecast).WithName("GetWeatherForecast").WithOpenApi();
// get some info about the gpio
app.MapGet("/gpio_info", GpioInfo).WithName("GetGpioInfo").WithOpenApi();
// manually add a state change
app.MapPost("/facility_state", handler: RegisterFacilityState).WithName("NewFacilityState").WithOpenApi();
// regularly sense temperature pair
app.MapPost("/sense_temperature", handler: SenseTemperature).WithName("NewSensorEvent").WithOpenApi();


app.Run();
log.LogInformation("After App Run");

#region API Methods
static async Task<IResult> getheartbeat(ILogger<Program> log){
    log.LogInformation(message: "heartbeat");
    return TypedResults.Ok(new{ foo="bar", at=DateTime.Now });

}

static async Task<IResult> getforecast(ILogger<Program> log)
{
    log.LogInformation("forecasting");
    var summaries = new[]
    {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return TypedResults.Ok(forecast);
}

static async Task<IResult> GpioInfo(HttpContext context, ILogger<Program> logger)
{
    if (Recorder.TryGetHardwareInfo(out var info, out var problem))
    {
        logger.LogInformation("requested gpio info: {i}", info);
        return TypedResults.Ok(info);
    }
    else
    {
        return Results.Problem(detail: problem?.Message ?? "Unknown message", statusCode: 500);
    }
}

static async Task<IResult> RegisterFacilityState(HttpContext context, ILogger<Program> logger, FacilityState state, Recorder recorder)
{
    try
    {
        logger.LogInformation("RegisterFacilityState");
        recorder.AddFacilityState(state);
        return TypedResults.Ok(new { DateTimeOffset.Now, state });
    }
    catch (Exception problem)
    {
        return Results.Problem(detail: problem?.Message ?? "Unknown message", statusCode: 500);
    }
}

static async Task<IResult> SenseTemperature(HttpContext context, Recorder recorder)
{
    if (Recorder.HasSensorHardware)
    {
        var tp = recorder.GetTemperaturePair();
        return TypedResults.Ok(tp);
    }
    else
    {
        return TypedResults.Problem(statusCode: 500);
    }
}
#endregion

#region Model records
namespace GardenSage.Recorder
{
    public record TemperatureDifference(DateTimeOffset Time, double Indoor, double Outdoor)
    {
    }

    public record FacilityState(bool AC, bool Furnace, bool Vent)
    {
    }

    record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
#endregion
