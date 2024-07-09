
using GardenSage.Common;
using GardenSage.Test.Mocks;

using Xunit.Abstractions;
namespace GardenSage.Test;

/// <summary>
/// Tests around the Synthetic Temperature model
/// </summary>
public class SynthTempTests(ITestOutputHelper testOutput)
{
    [Theory(Skip = "No testing occurs")]
    [InlineData(1, false)]
    [InlineData(0, true)]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
    public void TestName(double v, bool expectException)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
    {
        // Given
        SyntheticTemperature synth = new(66, 94, 26, 40);
        synth.Temperature(0);//.CarrierWave(1, v);
        // When

        // Then
    }

    /// <summary>
    /// write synthetic temperature data to csv files
    /// </summary>
    [Fact]
    public void YearTemperaturesToCSV()
    {
        int hoursperyear = 8760;
        DateTime yearstart = new(2000, month: 1, day: 1);
        var synth = SyntheticTemperature.Default;
        var tData = Enumerable.Range(0, hoursperyear)
            .Select(h => new
            {
                Hour = h,
                Date = yearstart.AddHours(h),
                Temperature = synth.Temperature(h),
                csv = $"{yearstart.AddHours(h).DayOfYear},{h},{synth.Temperature(h)}"
            });
        var csvlines = tData;
        // all hours of the year
        File.WriteAllLines("yeardata.all.csv", tData.Select(o => o.csv));
        // Sampled Weekly
        // every other hour for the first two days of each week
        File.WriteAllLines("yeardata.weekly.csv", tData.Where(o => o.Date.DayOfYear % 7 < 2 && o.Hour % 2 == 0).Select(o => o.csv));
        // Sampled and Compressed:
        // Every third hour, of the first 3 days in every month, BUT:
        // also reworks the csv, instead of <day,hour,temp> it is <day,entryindex,temp> (to compress the horizontal axis)
        File.WriteAllLines("yeardata.sampled.compressed.csv", tData.Where(o => o.Date.Day < 3 && o.Hour % 3 == 0)
            .Select((o, i) => $"{yearstart.AddHours(o.Hour).DayOfYear},{i},{o.Temperature}"));

        testOutput.WriteLine(synth.Metadata);
    }

    [Fact(Skip = "No testing occurs")]
    public void YearTemperaturesReflectReality()
    {
        var yearstart = new DateTime(2000, month: 1, day: 1);
        var tempfunc = (float h) => SyntheticTemperature.CarrierWave_Year(h);
        var ans = Enumerable.Range(0, SyntheticTemperature.HOURS_PER_YEAR)
            .Select(h => new
            {
                date = yearstart.AddHours(h),
                temp = tempfunc(h),
            })
            .GroupBy(o => o.date.DayOfYear)
            .Select(g => new
            {
                date = g.Key,
                max = g.Max(o => o.temp)
            });

    }
}
