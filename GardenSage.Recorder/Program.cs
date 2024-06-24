using System.Device.Gpio.Drivers;

using NLog;
using NLog.Extensions.Logging;
using NLog.Web;

using Thermosensor;

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
#pragma warning disable SDGPIO0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    try
    {
        System.Device.Gpio.GpioController ctrl = new();
        var info = ctrl.QueryComponentInformation();
        logger.LogInformation("requested gpio info: {i}", info);
        return TypedResults.Ok(info);
    }
    catch (GpiodException unsupported)
    {
        return Results.Problem(detail: unsupported.Message, statusCode: 500);
    }
#pragma warning restore SDGPIO0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}

static async Task<IResult> RegisterFacilityState(HttpContext context, FacilityState state, ILogger<Program> logger)
{
    logger.LogInformation("RegisterFacilityState: {state}", state);
    return TypedResults.Ok(new { DateTimeOffset.Now, state });
}

static async Task<IResult> SenseTemperature(HttpContext context, ILogger<Program> logger)
{
    var state = new
    {
        inside = Random.Shared.Next(50, 90),
        outside = Random.Shared.Next(50, 90)
    };
    logger.LogInformation("SenseTemperature: {state}", state);
    return TypedResults.Ok(new { DateTimeOffset.Now, state });
}
#endregion

#region Model records
record FacilityState(bool AC, bool Furnace, bool Vent)
{

}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
#endregion
