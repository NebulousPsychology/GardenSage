// See https://aka.ms/new-console-template for more information
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

using GardenSage.Common;
using GardenSage.Common.MeteoJson;
using GardenSage.Common.Options;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

HostApplicationBuilder hostbuilder = Host.CreateApplicationBuilder(args);
hostbuilder.Logging.AddSimpleConsole(options => options.IncludeScopes = true);
#region Conf
hostbuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>()
{
    // ["GardenSage:LocalJsonClientPath"] = "GardenSage.Test/data/fetch.json",
});

if (!hostbuilder.Environment.IsProduction())
{
    Console.WriteLine(hostbuilder.Configuration.GetDebugView());
}

hostbuilder.Services
    .Configure<ThermostatOptions>(hostbuilder.Configuration.GetSection("GardenSage:Thermostat"))
    .Configure<SageOptions>(hostbuilder.Configuration.GetSection("GardenSage"))
    .Configure<LocalJsonClient.LocalClientOptions>(hostbuilder.Configuration.GetSection("GardenSage:Client"))
    .Configure<JustTemperatureOption>(hostbuilder.Configuration.GetSection("GardenSage"))
    ;

#endregion Conf

#region Services

// The data fetch services
#if USE_OPENMETEO_LIB
hostbuilder.Services.AddSingleton<ISageWeatherClient, MeteoClient>();
// ...
#else
//// hostbuilder.Services.AddSingleton<ISageWeatherClient, LocalJsonClient>();
//// hostbuilder.Services.AddSingleton<ISageWeatherClient, MeteoJsonClient>();
// ISageWeatherClient is not managed by DI, ForecastRetrievalService handles construction based on config
hostbuilder.Services.AddSingleton<ForecastRetrievalService, ForecastRetrievalService>();
#endif


// add ephemeral gardening logic services
hostbuilder.Services
    .AddSingleton<ThermostatService>()
    .AddTransient<DryChoreReadinessCheckService>();
// and the entrypoint
hostbuilder.Services
    .AddHostedService<OptionEchoService>()
    .AddHostedService<ForecastAnalysisService>();
hostbuilder.Services.BuildServiceProvider(validateScopes: true);
#endregion Services

using IHost host = hostbuilder.Build();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
var pcon = host.Services.GetRequiredService<ILogger<Program>>();

pcon.LogDebug("Log ");

lifetime.ApplicationStarted.Register(() => pcon.LogInformation("Started"));
lifetime.ApplicationStopping.Register(() => pcon.LogTrace("Stopping"));
lifetime.ApplicationStopped.Register(() => pcon.LogInformation("Stopped"));

// host.Start(); // await host.WaitForShutdownAsync(); // // host.WaitForShutdown();// Listens for Ctrl+C.
await host.RunAsync();

class OptionEchoService(
    //IHostApplicationLifetime life,
    ILogger<OptionEchoService> logger,
    IOptions<ThermostatOptions> thermo,
    IOptions<SageOptions> sage,
    IOptions<LocalJsonClient.LocalClientOptions> client,
    IOptions<JustTemperatureOption> opts
) : BackgroundService
{
    readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Available IOptions {json}", JsonSerializer.Serialize(new
        {
            thermo = thermo.Value,
            sage = sage.Value,
            client = client.Value,
            opts = opts.Value,
        }, _options));
        // life.StopApplication();
        return Task.CompletedTask;
    }
}
