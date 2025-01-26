using System.Net;
using Microsoft.Azure.Functions.Worker.Http;

namespace WeatherStationImages.Authentication;

public class ApiKeyAuthHandler
{
    private const string ApiKeyHeaderName = "X-API-Key";

    public static async Task<(bool IsValid, HttpResponseData? ErrorResponse)> ValidateApiKeyAsync(HttpRequestData request)
    {
        if (!request.Headers.TryGetValues(ApiKeyHeaderName, out var apiKeyValues))
        {
            var response = request.CreateResponse(HttpStatusCode.Unauthorized);
            await response.WriteStringAsync("API Key is missing");
            return (false, response);
        }

        string? apiKey = apiKeyValues.FirstOrDefault();
        string? expectedApiKey = Environment.GetEnvironmentVariable("API_KEY");

        if (string.IsNullOrEmpty(apiKey) || apiKey != expectedApiKey)
        {
            var response = request.CreateResponse(HttpStatusCode.Unauthorized);
            await response.WriteStringAsync("Invalid API Key");
            return (false, response);
        }

        return (true, null);
    }
}