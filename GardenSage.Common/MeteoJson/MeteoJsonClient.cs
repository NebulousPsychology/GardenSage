using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;


namespace GardenSage.Common.MeteoJson;

/// <summary>
/// Fetch json from OpenMeteo webservice
/// </summary>
public class MeteoJsonClient : ISageWeatherClient
{
    private readonly HttpClient _client;

    public MeteoJsonClient(HttpClient client)
    {
        this._client = client;
    }

    public MeteoJsonClient()
    {
        HttpClientHandler handler = new()
        {
            AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
        };
        this._client = new HttpClient(new RetryHandler(handler));
        this._client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
    }

    /// <summary>
    /// Retry failed HTTP requests. See: https://stackoverflow.com/a/19650002
    /// </summary>
    class RetryHandler : DelegatingHandler
    {
        private const int MaxRetries = 3;
        private const double BackoffFactor = 0.5;
        private const int BackoffMaxSeconds = 2;


        public RetryHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        { }


        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage? response = null;
            for (int i = 0; i < MaxRetries; i++)
            {
                response = await base.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return response;
                }
                int waitMs = (int)Math.Min(BackoffFactor * Math.Pow(2, i), BackoffMaxSeconds) * 1000;
                await Task.Delay(waitMs);
            }
            return response!;
        }
    }

    public async Task<IForecastDataAdapter> GetWeatherAsync(ForecastParameters p)
    {
        Debug.Assert(p.format == OMDataFormat.json);
        p.format = OMDataFormat.json;// ex
        Uri uri = ForecastUriFactory.CreateUri(p);
        System.Console.WriteLine($"Get weather from {uri}");

#if DESERIALIZE_DIRECT_JSON
        ForecastData data = await _client.GetFromJsonAsync<ForecastData>(uri, ForecastData.JsonOptions)
            ?? throw new InvalidOperationException("NULL");
#else
        string json = await _client.GetStringAsync(uri);
        Debug.WriteLine(json);
        JsonForecastData data = JsonSerializer.Deserialize<JsonForecastData>(json, JsonForecastData.JsonOptions)
            ?? throw new InvalidDataException($"Failed to deserialize :::{json}:::");
#endif 
        return new MeteoJsonAdapter(data) { Retrieved = DateTimeOffset.Now };
    }
}
