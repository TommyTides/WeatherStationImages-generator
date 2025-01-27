using Microsoft.Azure.Functions.Worker;
using Azure.Storage.Queues;
using System.Text.Json;
using WeatherStationImages.Models;
using WeatherStationImages.Services;
using Microsoft.Extensions.Logging;

namespace WeatherStationImages.Functions;

public class WeatherFetchFunction
{
    private readonly IWeatherService _weatherService;
    private readonly HttpClient _httpClient;
    private readonly QueueClient _imageProcessQueue;
    private readonly ILogger<WeatherFetchFunction> _logger;
    private const string BUIENRADAR_URL = "https://data.buienradar.nl/2.0/feed/json";

    public WeatherFetchFunction(
        IWeatherService weatherService,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _weatherService = weatherService;
        _httpClient = httpClientFactory.CreateClient();
        _logger = loggerFactory.CreateLogger<WeatherFetchFunction>();

        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        _imageProcessQueue = new QueueClient(connectionString, "image-process");
        _imageProcessQueue.CreateIfNotExists();
    }

    [Function("WeatherFetch")]
    public async Task RunAsync([QueueTrigger("weather-fetch")] string messageContent)
    {
        try
        {
            var request = JsonSerializer.Deserialize<WeatherFetchRequest>(messageContent);
            if (request == null)
            {
                _logger.LogError("Failed to deserialize weather fetch request");
                return;
            }

            _logger.LogInformation("Fetching weather data from Buienradar for job {JobId}", request.JobId);
            var buienradarResponse = await _httpClient.GetStringAsync(BUIENRADAR_URL);
            var weatherDataList = _weatherService.ParseWeatherData(buienradarResponse);

            var totalImages = weatherDataList.Count;
            _logger.LogInformation("Found {Count} weather stations to process", totalImages);

            // Fan out to individual image processing tasks
            for (int i = 0; i < weatherDataList.Count; i++)
            {
                var imageRequest = new ImageProcessRequest
                {
                    JobId = request.JobId,
                    WeatherData = weatherDataList[i],
                    TotalImages = totalImages
                };

                var message = JsonSerializer.Serialize(imageRequest);
                var base64Message = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message));
                await _imageProcessQueue.SendMessageAsync(base64Message);

                _logger.LogInformation("Queued image processing task for station {Station}",
                    weatherDataList[i].StationName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing weather data: {MessageContent}", messageContent);
            throw;
        }
    }
}