using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
using WeatherStationImages.Models;
using WeatherStationImages.Authentication;
using Microsoft.Extensions.Logging;

namespace WeatherStationImages.Functions;

public class CreateImagesFunction
{
    private readonly QueueClient _weatherFetchQueue;
    private readonly ILogger<CreateImagesFunction> _logger;

    public CreateImagesFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<CreateImagesFunction>();

        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        _weatherFetchQueue = new QueueClient(connectionString ?? "UseDevelopmentStorage=true", "weather-fetch");
        _weatherFetchQueue.CreateIfNotExists();
    }

    [Function("CreateImages")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        try
        {
            // Validate API Key
            var (isValid, errorResponse) = await ApiKeyAuthHandler.ValidateApiKeyAsync(req);
            if (!isValid)
            {
                return errorResponse!;
            }

            var jobId = Guid.NewGuid().ToString();
            _logger.LogInformation("Creating new weather processing job: {JobId}", jobId);

            // Create and send weather fetch request
            var weatherFetchRequest = new WeatherFetchRequest { JobId = jobId };
            var message = JsonSerializer.Serialize(weatherFetchRequest);
            var base64Message = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message));

            await _weatherFetchQueue.SendMessageAsync(base64Message);
            _logger.LogInformation("Weather fetch request queued for job: {JobId}", jobId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                jobId,
                message = "Weather data fetch initiated",
                statusUrl = $"/api/status/{jobId}"
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating weather processing");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"An error occurred: {ex.Message}");
            return errorResponse;
        }
    }
}