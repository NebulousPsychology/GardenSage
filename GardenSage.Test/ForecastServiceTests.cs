using GardenSage.Common;
using GardenSage.Common.MeteoJson;
using GardenSage.Common.Options;
using GardenSage.Test.Mocks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.Extensions;

using System.Collections.Immutable;
using System.Text.Json;

using Xunit.Abstractions;
namespace GardenSage.Test;

public class ForecastServiceTests(ITestOutputHelper log)
{
    [Fact]
    public void AdaptDeserializingJson()
    {
        // Given
        string json = File.ReadAllText(LocalDeserializeTests.FULL_DOCUMENT_PATH);
        var data = JsonSerializer.Deserialize<JsonForecastData>(json, JsonForecastData.JsonOptions)
            ?? throw new InvalidOperationException("NULL");
        // When
        IForecastDataAdapter d = new MeteoJsonAdapter(data);
        // Then
    }

    [Theory]
    [InlineData(new int[0], new int[0], true, typeof(InvalidOperationException))]
    [InlineData(new int[0], new[] { 1 }, false)]
    [InlineData(new[] { 1 }, new[] { 1, }, true)]
    [InlineData(new[] { 1, 3, 5 }, new[] { 1, 2, 3, 4, 5 }, true)]
    public void Hindsight(int[] expected, int[] input, bool includeFirst, Type? ex = null)
    {
        bool PreviousWasEven(int prev, int cur) => prev % 2 == 0;
        IEnumerable<int> Example() => ThermostatService.HindsightedWhere(input, PreviousWasEven, includeFirst);
        if (ex is not null)
            Assert.Throws(ex, (Func<IEnumerable<int>>)Example);
        else
            Assert.Equal(expected, Example());
    }

    [Fact]
    public async void ForecastService_LocalTestData()
    {
        IHostBuilder builder = Host.CreateDefaultBuilder([]);
        builder.ConfigureHostConfiguration(cfg =>
        {
            cfg
            .AddJsonFile("appsettings.json")
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LocalJsonClientPath"] = LocalDeserializeTests.FULL_DOCUMENT_PATH,
            });
        });

        builder.ConfigureLogging(ilb => ilb.AddProvider(new XUnitTestOutputLoggerProvider(log)));
        builder.ConfigureServices(coll =>
        {
            coll.AddSingleton<ForecastRetrievalService, ForecastRetrievalService>();
            coll.AddTransient<ThermostatService>();
            coll.AddTransient<DryChoreReadinessCheckService>();
            coll.AddSingleton<ForecastAnalysisService>();
        });

        using var host = builder.Build();
        var tl = host.Services.GetRequiredService<ILogger<ForecastServiceTests>>();
        tl.LogInformation("Hello {test}", nameof(ForecastService_LocalTestData));
        var s = host.Services.GetRequiredService<ForecastAnalysisService>();
        // Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
        // ForecastService s = new ForecastService(logger: NullLoggerFactory.Instance, 
        //     client: new TestDataClient(LocalDeserializeTests.FULL_DOCUMENT_PATH), 
        //     lifetime: IHostApplicationLifetime.);
        await s.StartAsync(CancellationToken.None);
        // Given
        // foreach (var line in s.WriteForecast())
        // {
        //     log.WriteLine(line);
        // }

        // When
        // Then
        // Assert.Fail("log");
    }
    static ImmutableSortedDictionary<DateTimeOffset, T>
        CreateDataSpoof<T>(DateTimeOffset startday, Func<int, T> selector, int hours = 40) => Enumerable
            .Range(0, hours).ToImmutableSortedDictionary(
                keySelector: t => startday.AddHours(t),
                elementSelector: selector
                );

    /// <summary>
    /// vs 68:  rises 25..26, falls 27..28, rises 28..29
    /// </summary>
    static float Fake_risingtemps(int h) => h + 2 * MathF.Sin(h + 1) + 41.2f;

    /// <summary>
    /// vs 68: dips at h=28..29, rises at 30..31,  dips 32..33
    /// </summary>
    static float Fake_fallingtemps(int h) => -h + 2 * MathF.Sin(h + 1) + 98.5f;

    [Fact]
    public void ForecastSubstituted()
    {
        var logfactory = LoggerFactory.Create(b => b.AddProvider(new XUnitTestOutputLoggerProvider(log)));
        var startday = DateTimeOffset.Parse("1/01/2024 -0:00");
        // Given
        ILogger<ForecastServiceTests> testlog = logfactory.CreateLogger<ForecastServiceTests>();
        Assert.NotNull(testlog);
        testlog.LogInformation("A info is fired");
        testlog.LogWarning("A warn is fired");

        static IOptions<T> CreateOptionsSubstitute<T>(T instance) where T : class
        {
            var o = Substitute.For<IOptions<T>>();
            o.Value.Returns(instance);
            return o;
        }

        var sublife = Substitute.For<IHostApplicationLifetime>();
        // inject data
        var forcedata = Substitute.For<IForecastDataAdapter>();
        ImmutableSortedDictionary<DateTimeOffset, float> spoofData = CreateDataSpoof<float>(startday, Fake_risingtemps);
        testlog.LogInformation("working on {h} hours of synthetic data", spoofData.Count);
        forcedata.Temperature.Returns(returnThis: spoofData);
        Assert.NotNull(forcedata.Temperature);

        var subcli = Substitute.For<ISageWeatherClient>();
        subcli.GetWeatherAsync(new()).ReturnsForAnyArgs(forcedata);
        Assert.NotNull(subcli.GetWeatherAsync(new()));
        var sageOptions = CreateOptionsSubstitute(new SageOptions { });
        var cliOpts = CreateOptionsSubstitute<LocalJsonClient.LocalClientOptions>(new()
        {
            LocalClientPath = LocalDeserializeTests.FULL_DOCUMENT_PATH
        });

        ForecastRetrievalService datafetcher = new(logfactory, sageOptions, cliOpts);
        var thermOpts = CreateOptionsSubstitute<ThermostatOptions>(new() { });
        ThermostatService thermostat = new(
            logger: logfactory.CreateLogger<ThermostatService>(),
            options: thermOpts,
            temps: sageOptions,
            forecast: datafetcher);
        DryChoreReadinessCheckService dryservice = new(
            logger: logfactory.CreateLogger<DryChoreReadinessCheckService>(),
            forecast: datafetcher);

        ForecastAnalysisService fcastSvc = new(
            lifetime: sublife,
            logger: logfactory.CreateLogger<ForecastAnalysisService>(),
            options: sageOptions,
            datafetcher, dryservice, thermostat);

        // Todo: Consider PartsOf - Create a substitute for a class that behaves just like a real instance of the class, but also records calls made to its virtual members and allows for specific members to be substituted by using When(() => call).DoNotCallBase() or by setting a value to return value for that member.
        // var fcastSvc = Substitute.ForPartsOf<ForecastService>(subcli, sublog, sublife);
        // fcastSvc.When(async (c) => await c.StartAsync(CancellationToken.None)).DoNotCallBase();

        // When
        testlog.LogInformation("ENTER ACTION");
        IEnumerable<DateTimeOffset> closes = fcastSvc.CloseWindowsAt();
        IEnumerable<DateTimeOffset> opens = fcastSvc.OpenWindowsAt();
        testlog.LogInformation("ACTION Finished");

        // Then
        //TODO

        // open   {1/2/2024 1:00:00 AM +00:00} ok, hr 25..26 rises above 68
        // closes:{1/2/2024 3:00:00 AM +00:00} 27..28 falls below 68
        // open   {1/2/2024 4:00:00 AM +00:00} ok, hr 28..29 rises above 68

        // when the temperature is rising outside, I close the window when it rises above threshold, open it when it crosses below
        // when the temperature is falling outside, I close the window when it falls below threshold, open it when above
        // Assert.Equal([25, 28], opens.Select(kvp => (int)(kvp - startday).TotalHours));
        // Assert.Equal([27], closes.Select(kvp => (int)(kvp - startday).TotalHours));

        // DateTimeOffset? ans = fcastSvc.BestTimeToBlower(TimeSpan.FromMinutes(30), null);

        // fcastSvc.ReceivedWithAnyArgs().BestTimeToBlower(Arg.Any<TimeSpan>(), default);
        // Assert.Fail();
    }
}
