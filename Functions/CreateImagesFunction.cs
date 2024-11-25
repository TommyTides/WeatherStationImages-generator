using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
using WeatherStationImages.Models;
using WeatherStationImages.Services;
using Microsoft.Extensions.Logging;

namespace WeatherStationImages.Functions;

public class CreateImagesFunction
{
    private readonly IWeatherService _weatherService;
    private readonly QueueClient _queueClient;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CreateImagesFunction> _logger;
    private const string BUIENRADAR_URL = "https://data.buienradar.nl/2.0/feed/json";

    public CreateImagesFunction(
        IWeatherService weatherService,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _weatherService = weatherService;
        _httpClient = httpClientFactory.CreateClient();
        _logger = loggerFactory.CreateLogger<CreateImagesFunction>();

        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        _queueClient = new QueueClient(connectionString ?? "UseDevelopmentStorage=true", "start-processing");
        _queueClient.CreateIfNotExists();
    }

    [Function("CreateImages")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Fetching data from Buienradar");
            var buienradarResponse = await _httpClient.GetStringAsync(BUIENRADAR_URL);
            var weatherDataList = _weatherService.ParseWeatherData(buienradarResponse);

            if (!weatherDataList.Any())
            {
                _logger.LogWarning("No valid weather data found");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("No valid weather data found from Buienradar");
                return errorResponse;
            }

            var jobId = Guid.NewGuid().ToString();
            var totalImages = weatherDataList.Count;

            _logger.LogInformation("Starting job {JobId} with {TotalImages} images to process", jobId, totalImages);

            foreach (var weatherData in weatherDataList)
            {
                var message = new ImageGenerationRequest
                {
                    JobId = jobId,
                    WeatherData = weatherData,
                    TotalImages = totalImages
                };

                var messageJson = JsonSerializer.Serialize(message);
                var base64Message = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(messageJson));
                await _queueClient.SendMessageAsync(base64Message);

                _logger.LogInformation("Queued message for station: {StationName}", weatherData.StationName);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                jobId,
                message = $"Processing {totalImages} weather stations",
                statusUrl = $"/api/status/{jobId}"
            });

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch data from Buienradar");
            var errorResponse = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
            await errorResponse.WriteStringAsync($"Failed to fetch data from Buienradar: {ex.Message}");
            return errorResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"An error occurred: {ex.Message}");
            return errorResponse;
        }
    }
}