using GardenSage.Common;
using GardenSage.Common.MeteoJson;

using System.Text.Json;
namespace GardenSage.Test.Mocks;

// /// <summary>
// /// A MeteoJson Weather Client which reads from the ctor file path, NOT from the live service
// /// </summary>
// /// <param name="datapath"></param>
// [Obsolete(nameof(LocalJsonClient))]
// class TestDataClient : ISageWeatherClient
// {
//     public TestDataClient(string datapath)
//     {
//         Datapath = datapath;
//     }

//     public string Datapath { get; }

//     public async Task<IForecastDataAdapter> GetWeatherAsync(ForecastParameters p)
//     {
//         var json = await File.ReadAllTextAsync(Datapath);
//         var data = JsonSerializer.Deserialize<JsonForecastData>(json, JsonForecastData.JsonOptions)
//             ?? throw new InvalidOperationException("NULL");

//         return new MeteoJsonAdapter(data)
//         {
//             Retrieved = new DateTimeOffset(data
//                 .Daily.ResolveEnumerable<DateTime>("time")
//                 .Skip(2).First(),
//                 offset: TimeSpan.FromSeconds(data.UtcOffsetSeconds)),
//         };
//     }
// }
